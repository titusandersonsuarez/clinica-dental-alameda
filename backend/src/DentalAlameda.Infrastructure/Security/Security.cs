using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DentalAlameda.Infrastructure.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "DentalAlameda";
    public string Audience { get; set; } = "DentalAlameda.Web";
    /// <summary>Mínimo 32 caracteres. En producción va en variable de entorno Jwt__Key.</summary>
    public string Key { get; set; } = "";
    public int HorasExpiracion { get; set; } = 8;
}

public static class ClaimsPersonal
{
    public const string DebeCambiarPassword = "dcp";
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public (string Token, DateTime ExpiraEn) Generar(Usuario u)
    {
        var o = options.Value;
        var exp = DateTime.UtcNow.AddHours(o.HorasExpiracion);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)), SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, u.Email),
            new(ClaimTypes.Name, u.Nombre),
            new(ClaimTypes.Role, u.Rol)
        };
        // Mientras esté presente, la API solo permite cambiar la contraseña
        if (u.DebeCambiarPassword) claims.Add(new Claim(ClaimsPersonal.DebeCambiarPassword, "1"));
        var token = new JwtSecurityToken(o.Issuer, o.Audience, claims,
            expires: exp, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), exp);
    }
}

public class BogotaClock : IClock
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
    public DateTime AhoraLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);
}
