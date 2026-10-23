using DentalAlameda.Domain.Common;

namespace DentalAlameda.Domain.Entities;

public enum EstadoCita { Pendiente = 0, Confirmada = 1, Atendida = 2, Cancelada = 3 }

public class Cita
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int OdontologoId { get; set; }
    public Odontologo Odontologo { get; set; } = null!;
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string Motivo { get; set; } = "";
    public EstadoCita Estado { get; private set; } = EstadoCita.Pendiente;
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadaEn { get; private set; }
    public List<Notificacion> Notificaciones { get; set; } = [];

    public bool Activa => Estado is EstadoCita.Pendiente or EstadoCita.Confirmada;

    public bool SeCruzaCon(TimeOnly inicio, TimeOnly fin) => inicio < HoraFin && HoraInicio < fin;

    /// <summary>Transiciones permitidas: Pendiente→Confirmada→Atendida; Pendiente/Confirmada→Cancelada.</summary>
    public void CambiarEstado(EstadoCita nuevo)
    {
        var permitido = (Estado, nuevo) switch
        {
            (EstadoCita.Pendiente, EstadoCita.Confirmada) => true,
            (EstadoCita.Confirmada, EstadoCita.Atendida) => true,
            (EstadoCita.Pendiente, EstadoCita.Cancelada) => true,
            (EstadoCita.Confirmada, EstadoCita.Cancelada) => true,
            _ => false
        };
        if (!permitido)
            throw new DomainException($"No se puede pasar una cita de '{Estado}' a '{nuevo}'.");
        Estado = nuevo;
        ActualizadaEn = DateTime.UtcNow;
    }

    /// <summary>Encola el aviso de WhatsApp que corresponde al estado actual, si el paciente lo autorizó.</summary>
    public Notificacion? EncolarAvisoWhatsApp()
    {
        TipoNotificacion? tipo = Estado switch
        {
            EstadoCita.Confirmada => TipoNotificacion.ConfirmacionCita,
            EstadoCita.Cancelada => TipoNotificacion.CancelacionCita,
            _ => null
        };
        if (tipo is null || !Paciente.AceptaWhatsApp) return null;
        var telefono = TelefonoWhatsApp.AE164(Paciente.Celular);
        if (telefono is null) return null;

        var n = new Notificacion { Tipo = tipo.Value, Telefono = telefono };
        Notificaciones.Add(n);
        return n;
    }
}
