using System.Data;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DentalAlameda.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Odontologo> Odontologos => Set<Odontologo>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<RegistroClinico> RegistrosClinicos => Set<RegistroClinico>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder b) =>
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> operacion, CancellationToken ct = default)
    {
        if (!Database.IsRelational()) return await operacion();

        await using var tx = await Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var result = await operacion();
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (EsConflictoDeConcurrencia(ex))
        {
            await tx.RollbackAsync(ct);
            throw new DomainException("Ese horario se acaba de ocupar. Elige otro.");
        }
    }

    // 40001 = serialization_failure, 23505 = unique_violation (índice de cita única por odontólogo y hora)
    private static bool EsConflictoDeConcurrencia(Exception? ex)
    {
        for (; ex is not null; ex = ex.InnerException)
            if (ex is PostgresException { SqlState: "40001" or "23505" }) return true;
        return false;
    }
}
