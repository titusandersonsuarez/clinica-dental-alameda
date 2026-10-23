using DentalAlameda.Application.Abstractions;
using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalAlameda.Application.Services;

/// <summary>Envía los avisos de WhatsApp pendientes. Lo invoca NotificacionesWorker cada pocos segundos.</summary>
public class NotificacionesService(
    IAppDbContext db, INotificadorWhatsApp notificador, IOptions<ClinicaOptions> clinica, ILogger<NotificacionesService> log)
{
    // Nombres en español sin depender de ICU/cultura del servidor (contenedores en modo invariante)
    private static readonly string[] Dias = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];
    private static readonly string[] Meses = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    public static string FechaLarga(DateOnly f) => $"{Dias[(int)f.DayOfWeek]} {f.Day} de {Meses[f.Month - 1]}";
    public static string Hora12(TimeOnly t) => $"{(t.Hour % 12 == 0 ? 12 : t.Hour % 12)}:{t.Minute:00} {(t.Hour < 12 ? "a.m." : "p.m.")}";

    public async Task<int> ProcesarPendientesAsync(int lote = 20, CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var pendientes = await db.Notificaciones
            .Include(n => n.Cita).ThenInclude(c => c.Paciente)
            .Include(n => n.Cita).ThenInclude(c => c.Odontologo)
            .Where(n => n.Estado == EstadoNotificacion.Pendiente && n.ProximoIntentoEn <= ahora)
            .OrderBy(n => n.ProximoIntentoEn).Take(lote)
            .ToListAsync(ct);

        foreach (var n in pendientes)
        {
            // Si la cita cambió después de encolar (p. ej. se canceló), no se envía un aviso desactualizado
            var vigente = n.Tipo switch
            {
                TipoNotificacion.ConfirmacionCita => n.Cita.Estado == EstadoCita.Confirmada,
                TipoNotificacion.CancelacionCita => n.Cita.Estado == EstadoCita.Cancelada,
                _ => false
            };
            if (!vigente)
            {
                n.MarcarFallo("La cita cambió de estado antes del envío; aviso descartado.", reintentable: false, ahora);
                continue;
            }

            ResultadoEnvio r;
            try { r = await notificador.EnviarAsync(Construir(n), ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                r = ResultadoEnvio.Fallo(ex.Message, reintentable: true);
            }

            if (r.Ok) n.MarcarEnviada(r.IdProveedor, DateTime.UtcNow);
            else
            {
                n.MarcarFallo(r.Error ?? "Error desconocido", r.Reintentable, DateTime.UtcNow);
                log.LogWarning("WhatsApp de la cita {CitaId} falló (intento {Intento}): {Error}", n.CitaId, n.Intentos, r.Error);
            }
            await db.SaveChangesAsync(ct);
        }
        return pendientes.Count;
    }

    public MensajeWhatsApp Construir(Notificacion n)
    {
        var c = n.Cita;
        var nombre = c.Paciente.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? c.Paciente.Nombre;
        var fecha = FechaLarga(c.Fecha);
        var hora = Hora12(c.HoraInicio);
        var cl = clinica.Value;

        // Mismas variables para ambas plantillas: {{1}} nombre, {{2}} clínica, {{3}} fecha, {{4}} hora, {{5}} odontólogo, {{6}} dirección
        var vars = new Dictionary<string, string>
        {
            ["1"] = nombre, ["2"] = cl.Nombre, ["3"] = fecha, ["4"] = hora, ["5"] = c.Odontologo.Nombre, ["6"] = cl.Direccion
        };
        var texto = n.Tipo == TipoNotificacion.ConfirmacionCita
            ? $"Hola {nombre}, tu cita en {cl.Nombre} quedó confirmada para el {fecha} a las {hora} con {c.Odontologo.Nombre}. Dirección: {cl.Direccion}. Si no puedes asistir, responde a este mensaje."
            : $"Hola {nombre}, tu cita en {cl.Nombre} del {fecha} a las {hora} fue cancelada. Para reprogramar, responde a este mensaje o reserva de nuevo en nuestro sitio.";
        return new MensajeWhatsApp(n.Telefono, n.Tipo, vars, texto);
    }
}
