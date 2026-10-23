using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Dtos;
using DentalAlameda.Domain;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalAlameda.Application.Services;

public class AgendaService(IAppDbContext db, IClock clock)
{
    public const int DiasMaximosReserva = 60;

    public async Task<DisponibilidadDto> DisponibilidadAsync(int odontologoId, int servicioId, DateOnly fecha, CancellationToken ct = default)
    {
        var servicio = await ServicioActivo(servicioId, ct);
        await OdontologoActivo(odontologoId, ct);

        var ocupadas = await CitasActivasDelDia(odontologoId, fecha, ct);
        var ahora = clock.AhoraLocal;
        var hoy = DateOnly.FromDateTime(ahora);
        var horaActual = TimeOnly.FromDateTime(ahora);

        var horarios = HorarioClinica.InicioPosibles(fecha, servicio.DuracionMinutos)
            .Select(inicio =>
            {
                var fin = inicio.AddMinutes(servicio.DuracionMinutos);
                var pasado = fecha < hoy || (fecha == hoy && inicio <= horaActual);
                var cruzada = ocupadas.Any(c => inicio < c.HoraFin && c.HoraInicio < fin);
                return new HorarioDto(inicio.ToString("HH:mm"), !pasado && !cruzada);
            })
            .ToList();

        return new DisponibilidadDto(fecha, odontologoId, servicioId, horarios);
    }

    public async Task<CitaCreadaDto> ReservarAsync(CrearCitaRequest req, CancellationToken ct = default)
    {
        var servicio = await ServicioActivo(req.ServicioId, ct);
        var odontologo = await OdontologoActivo(req.OdontologoId, ct);

        if (!TimeOnly.TryParseExact(req.Hora, "HH:mm", out var inicio))
            throw new DomainException("La hora debe tener formato HH:mm.");
        var fin = inicio.AddMinutes(servicio.DuracionMinutos);

        var ahora = clock.AhoraLocal;
        var hoy = DateOnly.FromDateTime(ahora);
        if (req.Fecha < hoy || (req.Fecha == hoy && inicio <= TimeOnly.FromDateTime(ahora)))
            throw new DomainException("No se pueden reservar horarios que ya pasaron.");
        if (req.Fecha > hoy.AddDays(DiasMaximosReserva))
            throw new DomainException($"Solo se puede reservar con hasta {DiasMaximosReserva} días de anticipación.");
        if (!HorarioClinica.EsHorarioValido(req.Fecha, inicio, servicio.DuracionMinutos))
            throw new DomainException("Ese horario está fuera de la jornada de atención para este servicio.");

        if (!req.AceptaPoliticaDatos)
            throw new DomainException("Para reservar debes aceptar la política de tratamiento de datos.");
        var celular = SoloDigitos(req.Celular);

        return await db.EnTransaccionAsync(async () =>
        {
            var ocupadas = await CitasActivasDelDia(odontologo.Id, req.Fecha, ct);
            if (ocupadas.Any(c => inicio < c.HoraFin && c.HoraInicio < fin))
                throw new DomainException("Ese horario se acaba de ocupar. Elige otro.");

            var paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Cedula == req.Cedula, ct);
            if (paciente is null)
            {
                paciente = new Paciente { Cedula = req.Cedula, Nombre = req.Nombre.Trim(), Celular = celular, Correo = req.Correo?.Trim(), AceptaWhatsApp = req.AceptaWhatsApp, AutorizaDatosEn = DateTime.UtcNow };
                db.Pacientes.Add(paciente);
            }
            else
            {
                var pacienteTieneCitaCruzada = await db.Citas.AnyAsync(c =>
                    c.PacienteId == paciente.Id && c.Fecha == req.Fecha &&
                    (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada) &&
                    inicio < c.HoraFin && c.HoraInicio < fin, ct);
                if (pacienteTieneCitaCruzada)
                    throw new DomainException("Ya tienes una cita a esa hora.");
                paciente.ActualizarContacto(req.Nombre, celular, req.Correo);
                paciente.AceptaWhatsApp = req.AceptaWhatsApp;
                paciente.AutorizaDatosEn = DateTime.UtcNow;
            }

            var cita = new Cita
            {
                Paciente = paciente,
                OdontologoId = odontologo.Id,
                ServicioId = servicio.Id,
                Fecha = req.Fecha,
                HoraInicio = inicio,
                HoraFin = fin,
                Motivo = req.Motivo?.Trim() ?? ""
            };
            db.Citas.Add(cita);
            await db.SaveChangesAsync(ct);

            return new CitaCreadaDto(cita.Id, cita.Fecha, inicio.ToString("HH:mm"), servicio.Nombre, odontologo.Nombre, cita.Estado.ToString());
        }, ct);
    }

    /// <summary>Consulta pública de citas futuras: exige cédula y celular para no exponer datos.</summary>
    public async Task<IReadOnlyList<CitaPacienteDto>> MisCitasAsync(string cedula, string celular, CancellationToken ct = default)
    {
        var digitos = SoloDigitos(celular);
        var hoy = DateOnly.FromDateTime(clock.AhoraLocal);
        var citas = await db.Citas.AsNoTracking()
            .Where(c => c.Paciente.Cedula == cedula && c.Paciente.Celular == digitos && c.Fecha >= hoy)
            .OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio)
            .Select(c => new { c.Id, c.Fecha, c.HoraInicio, Servicio = c.Servicio.Nombre, Odontologo = c.Odontologo.Nombre, c.Estado })
            .ToListAsync(ct);
        return citas.Select(c => new CitaPacienteDto(c.Id, c.Fecha, c.HoraInicio.ToString("HH:mm"), c.Servicio, c.Odontologo, c.Estado.ToString())).ToList();
    }

    private Task<List<Cita>> CitasActivasDelDia(int odontologoId, DateOnly fecha, CancellationToken ct) =>
        db.Citas.Where(c => c.OdontologoId == odontologoId && c.Fecha == fecha &&
                            (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada))
                .ToListAsync(ct);

    private async Task<Servicio> ServicioActivo(int id, CancellationToken ct) =>
        await db.Servicios.FirstOrDefaultAsync(s => s.Id == id && s.Activo, ct)
        ?? throw new NotFoundException("El servicio no existe.");

    private async Task<Odontologo> OdontologoActivo(int id, CancellationToken ct) =>
        await db.Odontologos.FirstOrDefaultAsync(o => o.Id == id && o.Activo, ct)
        ?? throw new NotFoundException("El odontólogo no existe.");

    internal static string SoloDigitos(string s) => new(s.Where(char.IsDigit).ToArray());
}
