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
