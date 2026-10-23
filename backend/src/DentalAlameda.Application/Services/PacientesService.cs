using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Dtos;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalAlameda.Application.Services;

/// <summary>Historia clínica. Toda consulta y cambio queda en la bitácora de auditoría.</summary>
public class PacientesService(IAppDbContext db, ICurrentUser usuario)
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

        Auditar(AccionAuditoria.ConsultaHistoria, p.Id, $"Consultó la historia de {p.Nombre}");
        await db.SaveChangesAsync(ct);

        return new PacienteDetalleDto(p.Id, p.Nombre, p.Cedula, p.Celular, p.Correo, p.Antecedentes,
            p.Historia.OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id).Select(ADto).ToList(),
            p.Citas.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.HoraInicio)
                .Select(c => new CitaPacienteDto(c.Id, c.Fecha, c.HoraInicio.ToString("HH:mm"), c.Servicio.Nombre, c.Odontologo.Nombre, c.Estado.ToString())).ToList());
    }

    public async Task ActualizarAntecedentesAsync(int id, string antecedentes, CancellationToken ct = default)
    {
        var p = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("El paciente no existe.");
        var nuevo = antecedentes?.Trim() ?? "";
        if (nuevo == p.Antecedentes) return;
        Auditar(AccionAuditoria.AntecedentesModificados, p.Id, $"Antes: \"{Recortar(p.Antecedentes)}\" · Ahora: \"{Recortar(nuevo)}\"");
        p.Antecedentes = nuevo;
        await db.SaveChangesAsync(ct);
    }

    public async Task<RegistroClinicoDto> AgregarRegistroAsync(int id, CrearRegistroRequest req, CancellationToken ct = default)
    {
        var p = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("El paciente no existe.");
        Odontologo? odontologo = null;
        if (req.OdontologoId is { } oid)
            odontologo = await db.Odontologos.FirstOrDefaultAsync(o => o.Id == oid, ct)
                         ?? throw new NotFoundException("El odontólogo no existe.");

        var r = p.AgregarRegistro(req.Fecha, req.PiezaFdi, req.Procedimiento, req.Observaciones, req.OdontologoId, usuario.Id, usuario.Nombre);
        r.Odontologo = odontologo;
        db.RegistrosClinicos.Add(r);
        Auditar(AccionAuditoria.RegistroCreado, p.Id,
            $"{r.Procedimiento}{(r.PiezaFdi is null ? "" : $" (pieza {r.PiezaFdi})")} con fecha {r.Fecha:yyyy-MM-dd}");
        await db.SaveChangesAsync(ct);
        return ADto(r);
    }

    /// <summary>Los registros no se editan ni se borran: se anulan con motivo y quedan visibles tachados.</summary>
    public async Task<RegistroClinicoDto> AnularRegistroAsync(int pacienteId, int registroId, string motivo, CancellationToken ct = default)
    {
        var r = await db.RegistrosClinicos.Include(x => x.Odontologo)
                    .FirstOrDefaultAsync(x => x.Id == registroId && x.PacienteId == pacienteId, ct)
                ?? throw new NotFoundException("El registro no existe.");
        r.Anular(motivo, usuario.Nombre);
        Auditar(AccionAuditoria.RegistroAnulado, pacienteId, $"Anuló \"{r.Procedimiento}\" del {r.Fecha:yyyy-MM-dd}. Motivo: {r.MotivoAnulacion}");
        await db.SaveChangesAsync(ct);
        return ADto(r);
    }

    private void Auditar(AccionAuditoria accion, int pacienteId, string detalle) =>
        db.Auditoria.Add(new AuditoriaClinica
        {
            UsuarioId = usuario.Id, UsuarioNombre = usuario.Nombre, UsuarioRol = usuario.Rol,
            Accion = accion, PacienteId = pacienteId, Detalle = Recortar(detalle, 1000), Ip = usuario.Ip
        });

    private static string Recortar(string s, int max = 200) => s.Length <= max ? s : s[..max] + "…";

    private static RegistroClinicoDto ADto(RegistroClinico h) => new(h.Id, h.Fecha, h.PiezaFdi, h.Procedimiento, h.Observaciones,
        h.Odontologo?.Nombre, h.AutorNombre, h.CreadoEn, h.Anulado, h.MotivoAnulacion, h.AnuladoPorNombre, h.AnuladoEn);
}

public class AuditoriaService(IAppDbContext db)
{
    public async Task<IReadOnlyList<AuditoriaDto>> ListarAsync(int? pacienteId, DateOnly? desde, DateOnly? hasta, string? accion, CancellationToken ct = default)
    {
        var q = db.Auditoria.AsNoTracking().AsQueryable();
        if (pacienteId is { } pid) q = q.Where(a => a.PacienteId == pid);
        if (desde is { } d) { var dt = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(5); q = q.Where(a => a.Fecha >= dt); }
        if (hasta is { } h) { var ht = h.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(5); q = q.Where(a => a.Fecha < ht); }
        if (!string.IsNullOrWhiteSpace(accion) && Enum.TryParse<AccionAuditoria>(accion, true, out var ac)) q = q.Where(a => a.Accion == ac);

        var filas = await q.OrderByDescending(a => a.Id).Take(500)
            .Join(db.Pacientes, a => a.PacienteId, p => p.Id, (a, p) => new { a, Paciente = p.Nombre })
            .ToListAsync(ct);
        return filas.Select(x => new AuditoriaDto(x.a.Id, x.a.Fecha, x.a.UsuarioNombre, x.a.UsuarioRol, x.a.Accion.ToString(),
            x.a.PacienteId, x.Paciente, x.a.Detalle, x.a.Ip)).ToList();
    }
}
