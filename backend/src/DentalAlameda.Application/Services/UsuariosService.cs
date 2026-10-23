using System.Security.Cryptography;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Dtos;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalAlameda.Application.Services;

public class AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email, ct);
        if (u is null || !hasher.Verify(req.Password, u.PasswordHash))
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        if (!u.Activo)
            throw new UnauthorizedAccessException("Tu usuario está desactivado. Habla con el administrador.");
        u.UltimoIngresoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Respuesta(u);
    }

    public async Task<LoginResponse> CambiarPasswordAsync(int usuarioId, CambiarPasswordRequest req, CancellationToken ct = default)
    {
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId && x.Activo, ct)
                ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Ingresa de nuevo.");
        if (!hasher.Verify(req.Actual, u.PasswordHash))
            throw new DomainException("La contraseña actual no es correcta.");
        if (PoliticaPassword.Validar(req.Nueva) is { } error)
            throw new DomainException(error);
        if (req.Nueva == req.Actual)
            throw new DomainException("La nueva contraseña debe ser distinta a la actual.");

        u.PasswordHash = hasher.Hash(req.Nueva);
        u.DebeCambiarPassword = false;
        await db.SaveChangesAsync(ct);
        return Respuesta(u);
    }

    private LoginResponse Respuesta(Usuario u)
    {
        var (token, exp) = tokens.Generar(u);
        return new LoginResponse(token, exp, u.Nombre, u.Rol, u.DebeCambiarPassword);
    }
}

/// <summary>Gestión del personal (solo Admin).</summary>
public class UsuariosService(IAppDbContext db, IPasswordHasher hasher, ICurrentUser actual)
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct = default) =>
        await db.Usuarios.AsNoTracking().OrderBy(u => u.Nombre)
            .Select(u => new UsuarioDto(u.Id, u.Email, u.Nombre, u.Rol, u.Activo, u.DebeCambiarPassword,
                u.OdontologoId, u.Odontologo != null ? u.Odontologo.Nombre : null, u.UltimoIngresoEn))
            .ToListAsync(ct);

    public async Task<UsuarioConPasswordDto> CrearAsync(CrearUsuarioRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (!Roles.Todos.Contains(req.Rol))
            throw new DomainException("Rol no válido. Usa Admin, Recepcion u Odontologo.");
        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
            throw new DomainException("Ya existe un usuario con ese correo.");
        var odontologoId = await ValidarOdontologo(req.Rol, req.OdontologoId, ct);

        var temporal = GenerarTemporal();
        var u = new Usuario
        {
            Email = email, Nombre = req.Nombre.Trim(), Rol = req.Rol, OdontologoId = odontologoId,
            PasswordHash = hasher.Hash(temporal), DebeCambiarPassword = true
        };
        db.Usuarios.Add(u);
        await db.SaveChangesAsync(ct);
        return new UsuarioConPasswordDto(await Dto(u.Id, ct), temporal);
    }

    public async Task<UsuarioConPasswordDto> RestablecerPasswordAsync(int id, CancellationToken ct = default)
    {
        var u = await Buscar(id, ct);
        var temporal = GenerarTemporal();
        u.PasswordHash = hasher.Hash(temporal);
        u.DebeCambiarPassword = true;
        await db.SaveChangesAsync(ct);
        return new UsuarioConPasswordDto(await Dto(u.Id, ct), temporal);
    }

    public async Task CambiarActivoAsync(int id, bool activo, CancellationToken ct = default)
    {
        var u = await Buscar(id, ct);
        if (!activo)
        {
            if (u.Id == actual.Id) throw new DomainException("No puedes desactivar tu propio usuario.");
            if (u.Rol == Roles.Admin && await db.Usuarios.CountAsync(x => x.Rol == Roles.Admin && x.Activo, ct) <= 1)
                throw new DomainException("Debe quedar al menos un administrador activo.");
        }
        u.Activo = activo;
        await db.SaveChangesAsync(ct);
    }

    private async Task<int?> ValidarOdontologo(string rol, int? odontologoId, CancellationToken ct)
    {
        if (rol != Roles.Odontologo) return null;
        if (odontologoId is null) throw new DomainException("Un usuario odontólogo debe estar vinculado a un profesional de la agenda.");
        if (!await db.Odontologos.AnyAsync(o => o.Id == odontologoId, ct)) throw new NotFoundException("El odontólogo no existe.");
        return odontologoId;
    }

    private async Task<Usuario> Buscar(int id, CancellationToken ct) =>
        await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw new NotFoundException("El usuario no existe.");

    private async Task<UsuarioDto> Dto(int id, CancellationToken ct) => (await ListarAsync(ct)).Single(u => u.Id == id);

    /// <summary>Contraseña temporal legible: 12 caracteres sin letras ambiguas, con al menos un número.</summary>
    internal static string GenerarTemporal()
    {
        const string letras = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        const string numeros = "23456789";
        var chars = new char[12];
        for (var i = 0; i < chars.Length; i++)
        {
            var set = i % 4 == 3 ? numeros : letras;
            chars[i] = set[RandomNumberGenerator.GetInt32(set.Length)];
        }
        return new string(chars);
    }
}
