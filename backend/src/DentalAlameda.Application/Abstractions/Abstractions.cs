using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalAlameda.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Servicio> Servicios { get; }
    DbSet<Odontologo> Odontologos { get; }
    DbSet<Paciente> Pacientes { get; }
    DbSet<Cita> Citas { get; }
    DbSet<RegistroClinico> RegistrosClinicos { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<Notificacion> Notificaciones { get; }
    DbSet<AuditoriaClinica> Auditoria { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    /// <summary>Ejecuta la operación en una transacción serializable (evita reservas dobles).</summary>
    Task<T> EnTransaccionAsync<T>(Func<Task<T>> operacion, CancellationToken ct = default);
}

/// <summary>Hora local de la clínica (America/Bogota), inyectable para pruebas.</summary>
public interface IClock
{
    DateTime AhoraLocal { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiraEn) Generar(Usuario usuario);
}

/// <summary>Mensaje listo para enviar. Variables numeradas "1", "2"… como en las plantillas de WhatsApp.</summary>
public record MensajeWhatsApp(string Telefono, TipoNotificacion Tipo, IReadOnlyDictionary<string, string> Variables, string Texto);

/// <summary>Resultado del proveedor. Reintentable = error temporal (red, 429, 5xx).</summary>
public record ResultadoEnvio(bool Ok, string? IdProveedor = null, string? Error = null, bool Reintentable = false)
{
    public static ResultadoEnvio Exito(string? id) => new(true, id);
    public static ResultadoEnvio Fallo(string error, bool reintentable) => new(false, null, error, reintentable);
}

public interface INotificadorWhatsApp
{
    Task<ResultadoEnvio> EnviarAsync(MensajeWhatsApp mensaje, CancellationToken ct = default);
}

/// <summary>Datos de la clínica que aparecen en los mensajes (sección "Clinica" de appsettings).</summary>
public class ClinicaOptions
{
    public string Nombre { get; set; } = "Clínica Dental Alameda";
    public string Direccion { get; set; } = "";
}

/// <summary>Usuario autenticado de la petición actual (lo implementa la API a partir del JWT).</summary>
public interface ICurrentUser
{
    int? Id { get; }
    string Nombre { get; }
    string Rol { get; }
    string? Ip { get; }
}
