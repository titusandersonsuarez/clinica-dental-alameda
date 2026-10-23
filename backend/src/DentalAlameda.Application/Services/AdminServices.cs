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
                c.ServicioId, Servicio = c.Servicio.Nombre, c.OdontologoId, Odontologo = c.Odontologo.Nombre, c.Motivo
            }).ToListAsync(ct);

        return rows.Select(r => new CitaAdminDto(r.Id, r.Fecha, r.HoraInicio.ToString("HH:mm"), r.HoraFin.ToString("HH:mm"),
            r.Estado.ToString(), r.PacienteId, r.PacienteNombre, r.Cedula, r.Celular, r.ServicioId, r.Servicio,
            r.OdontologoId, r.Odontologo, r.Motivo)).ToList();
    }

    public async Task CambiarEstadoAsync(int id, EstadoCita estado, CancellationToken ct = default)
    {
        var cita = await db.Citas.FirstOrDefaultAsync(c => c.Id == id, ct)
                   ?? throw new NotFoundException("La cita no existe.");
        cita.CambiarEstado(estado);
        await db.SaveChangesAsync(ct);
    }

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

public class PacientesService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PacienteListaDto>> ListarAsync(string? q, CancellationToken ct = default)
    {
        var query = db.Pacientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(term) || p.Cedula.Contains(term));
        }
        return await query.OrderBy(p => p.Nombre).Take(200)
            .Select(p => new PacienteListaDto(p.Id, p.Nombre, p.Cedula, p.Celular, p.Citas.Count))
            .ToListAsync(ct);
    }

    public async Task<PacienteDetalleDto> DetalleAsync(int id, CancellationToken ct = default)
    {
        var p = await db.Pacientes.AsNoTracking()
                    .Include(x => x.Historia).ThenInclude(h => h.Odontologo)
                    .Include(x => x.Citas).ThenInclude(c => c.Servicio)
                    .Include(x => x.Citas).ThenInclude(c => c.Odontologo)
                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("El paciente no existe.");

        return new PacienteDetalleDto(p.Id, p.Nombre, p.Cedula, p.Celular, p.Correo, p.Antecedentes,
            p.Historia.OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
                .Select(h => new RegistroClinicoDto(h.Id, h.Fecha, h.PiezaFdi, h.Procedimiento, h.Observaciones, h.Odontologo?.Nombre)).ToList(),
            p.Citas.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.HoraInicio)
                .Select(c => new CitaPacienteDto(c.Id, c.Fecha, c.HoraInicio.ToString("HH:mm"), c.Servicio.Nombre, c.Odontologo.Nombre, c.Estado.ToString())).ToList());
    }

    public async Task ActualizarAntecedentesAsync(int id, string antecedentes, CancellationToken ct = default)
    {
        var p = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("El paciente no existe.");
        p.Antecedentes = antecedentes?.Trim() ?? "";
        await db.SaveChangesAsync(ct);
    }

    public async Task<RegistroClinicoDto> AgregarRegistroAsync(int id, CrearRegistroRequest req, CancellationToken ct = default)
    {
        var p = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("El paciente no existe.");
        string? odontologo = null;
        if (req.OdontologoId is { } oid)
            odontologo = (await db.Odontologos.FirstOrDefaultAsync(o => o.Id == oid, ct)
                          ?? throw new NotFoundException("El odontólogo no existe.")).Nombre;

        var r = p.AgregarRegistro(req.Fecha, req.PiezaFdi, req.Procedimiento, req.Observaciones, req.OdontologoId);
        db.RegistrosClinicos.Add(r);
        await db.SaveChangesAsync(ct);
        return new RegistroClinicoDto(r.Id, r.Fecha, r.PiezaFdi, r.Procedimiento, r.Observaciones, odontologo);
    }
}

public class AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email, ct);
        if (u is null || !hasher.Verify(req.Password, u.PasswordHash))
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        var (token, exp) = tokens.Generar(u);
        return new LoginResponse(token, exp, u.Nombre, u.Rol);
    }
}
