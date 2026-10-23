using System.ComponentModel.DataAnnotations;
using DentalAlameda.Domain.Entities;

namespace DentalAlameda.Application.Dtos;

// ---------- Catálogo ----------
public record ServicioDto(int Id, string Codigo, string Nombre, string Descripcion, int DuracionMinutos, decimal PrecioDesde);
public record OdontologoDto(int Id, string Nombre, string Especialidad, string RegistroProfesional);

// ---------- Agenda pública ----------
public record HorarioDto(string Hora, bool Disponible);
public record DisponibilidadDto(DateOnly Fecha, int OdontologoId, int ServicioId, IReadOnlyList<HorarioDto> Horarios);

public record CrearCitaRequest
{
    [Required] public int ServicioId { get; init; }
    [Required] public int OdontologoId { get; init; }
    [Required] public DateOnly Fecha { get; init; }
    [Required, RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "La hora debe tener formato HH:mm.")]
    public string Hora { get; init; } = "";
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; init; } = "";
    [Required, RegularExpression(@"^\d{6,11}$", ErrorMessage = "La cédula debe tener entre 6 y 11 dígitos, sin puntos.")]
    public string Cedula { get; init; } = "";
    [Required, RegularExpression(@"^[0-9 +]{10,15}$", ErrorMessage = "Escribe un celular válido (10 dígitos).")]
    public string Celular { get; init; } = "";
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string? Correo { get; init; }
    [StringLength(500)] public string? Motivo { get; init; }
    /// <summary>Autorización expresa para recibir avisos por WhatsApp.</summary>
    public bool AceptaWhatsApp { get; init; }
}

public record CitaCreadaDto(int Id, DateOnly Fecha, string Hora, string Servicio, string Odontologo, string Estado);

public record CitaPacienteDto(int Id, DateOnly Fecha, string Hora, string Servicio, string Odontologo, string Estado);

// ---------- Admin: citas ----------
public record CitaAdminDto(
    int Id, DateOnly Fecha, string HoraInicio, string HoraFin, string Estado,
    int PacienteId, string PacienteNombre, string Cedula, string Celular,
    int ServicioId, string Servicio, int OdontologoId, string Odontologo, string Motivo,
    bool AceptaWhatsApp, string? WhatsApp, string? WhatsAppError);

public record CitasFiltro(string? Estado, int? OdontologoId, DateOnly? Desde, DateOnly? Hasta, string? Q);

public record CambiarEstadoRequest([Required] EstadoCita Estado);

public record ResumenDto(int CitasHoy, int Pendientes, int Confirmadas, int Pacientes);

// ---------- Admin: pacientes ----------
public record PacienteListaDto(int Id, string Nombre, string Cedula, string Celular, int TotalCitas);

public record RegistroClinicoDto(int Id, DateOnly Fecha, string? PiezaFdi, string Procedimiento, string Observaciones, string? Odontologo);

public record PacienteDetalleDto(
    int Id, string Nombre, string Cedula, string Celular, string? Correo, string Antecedentes,
    IReadOnlyList<RegistroClinicoDto> Historia, IReadOnlyList<CitaPacienteDto> Citas);

public record AntecedentesRequest([StringLength(2000)] string Antecedentes);

public record CrearRegistroRequest
{
    [Required] public DateOnly Fecha { get; init; }
    public string? PiezaFdi { get; init; }
    [Required(ErrorMessage = "El procedimiento es obligatorio."), StringLength(200)]
    public string Procedimiento { get; init; } = "";
    [StringLength(2000)] public string? Observaciones { get; init; }
    public int? OdontologoId { get; init; }
}

// ---------- Auth ----------
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record LoginResponse(string Token, DateTime ExpiraEn, string Nombre, string Rol);
