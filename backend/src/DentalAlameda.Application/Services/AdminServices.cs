using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Dtos;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalAlameda.Application.Services;

public class CatalogoService(IAppDbContext db)
{
    public async Task<IReadOnlyList<ServicioDto>> ServiciosAsync(CancellationToken ct = default) =>
        await db.Servicios.AsNoTracking().Where(s => s.Activo).OrderBy(s => s.Id)
            .Select(s => new ServicioDto(s.Id, s.Codigo, s.Nombre, s.Descripcion, s.DuracionMinutos, s.PrecioDesde))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OdontologoDto>> OdontologosAsync(CancellationToken ct = default) =>
        await db.Odontologos.AsNoTracking().Where(o => o.Activo).OrderBy(o => o.Id)
            .Select(o => new OdontologoDto(o.Id, o.Nombre, o.Especialidad, o.RegistroProfesional))
            .ToListAsync(ct);
}

public class CitasAdminService(IAppDbContext db, IClock clock)
{
    public async Task<IReadOnlyList<CitaAdminDto>> ListarAsync(CitasFiltro f, CancellationToken ct = default)
    {
        var q = db.Citas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.Estado) && Enum.TryParse<EstadoCita>(f.Estado, true, out var estado))
            q = q.Where(c => c.Estado == estado);
        if (f.OdontologoId is { } od) q = q.Where(c => c.OdontologoId == od);
        if (f.Desde is { } d) q = q.Where(c => c.Fecha >= d);
        if (f.Hasta is { } h) q = q.Where(c => c.Fecha <= h);
        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var term = f.Q.Trim().ToLower();
            q = q.Where(c => c.Paciente.Nombre.ToLower().Contains(term) || c.Paciente.Cedula.Contains(term));
        }

        var rows = await q.OrderBy(c => c.Fecha).ThenBy(c => c.HoraInicio).Take(500)
            .Select(c => new
            {
                c.Id, c.Fecha, c.HoraInicio, c.HoraFin, c.Estado, c.PacienteId,
                PacienteNombre = c.Paciente.Nombre, c.Paciente.Cedula, c.Paciente.Celular,
                c.ServicioId, Servicio = c.Servicio.Nombre, c.OdontologoId, Odontologo = c.Odontologo.Nombre, c.Motivo,
                c.Paciente.AceptaWhatsApp,
                Aviso = c.Notificaciones.OrderByDescending(n => n.Id).Select(n => new { n.Estado, n.UltimoError }).FirstOrDefault()
            }).ToListAsync(ct);

        return rows.Select(r => new CitaAdminDto(r.Id, r.Fecha, r.HoraInicio.ToString("HH:mm"), r.HoraFin.ToString("HH:mm"),
            r.Estado.ToString(), r.PacienteId, r.PacienteNombre, r.Cedula, r.Celular, r.ServicioId, r.Servicio,
            r.OdontologoId, r.Odontologo, r.Motivo, r.AceptaWhatsApp,
            r.Aviso?.Estado.ToString(), r.Aviso?.UltimoError)).ToList();
    }

    public async Task CambiarEstadoAsync(int id, EstadoCita estado, CancellationToken ct = default)
    {
        var cita = await CitaConPaciente(id, ct);
        cita.CambiarEstado(estado);
        // Outbox: el aviso se guarda en la misma transacción y lo envía NotificacionesWorker
        cita.EncolarAvisoWhatsApp();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Vuelve a encolar el aviso de WhatsApp del estado actual (p. ej. tras un fallo).</summary>
    public async Task ReenviarWhatsAppAsync(int id, CancellationToken ct = default)
    {
        var cita = await CitaConPaciente(id, ct);
        if (cita.Estado is not (EstadoCita.Confirmada or EstadoCita.Cancelada))
            throw new DomainException("Solo se envía WhatsApp para citas confirmadas o canceladas.");
        if (!cita.Paciente.AceptaWhatsApp)
            throw new DomainException("El paciente no autorizó avisos por WhatsApp.");
        if (cita.EncolarAvisoWhatsApp() is null)
            throw new DomainException("El celular del paciente no es un número de WhatsApp válido.");
        await db.SaveChangesAsync(ct);
    }

    private async Task<Cita> CitaConPaciente(int id, CancellationToken ct) =>
        await db.Citas.Include(c => c.Paciente).FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new NotFoundException("La cita no existe.");

    public async Task<ResumenDto> ResumenAsync(CancellationToken ct = default)
    {
        var hoy = DateOnly.FromDateTime(clock.AhoraLocal);
        return new ResumenDto(
            await db.Citas.CountAsync(c => c.Fecha == hoy && (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada), ct),
            await db.Citas.CountAsync(c => c.Estado == EstadoCita.Pendiente && c.Fecha >= hoy, ct),
            await db.Citas.CountAsync(c => c.Estado == EstadoCita.Confirmada && c.Fecha >= hoy, ct),
            await db.Pacientes.CountAsync(ct));
    }
}

