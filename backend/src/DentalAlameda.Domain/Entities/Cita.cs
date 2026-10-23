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
}
