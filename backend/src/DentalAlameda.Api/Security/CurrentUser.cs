using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Infrastructure.Security;

namespace DentalAlameda.Api.Security;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public int? Id => int.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string Nombre => User?.FindFirstValue(ClaimTypes.Name) ?? "Sistema";
    public string Rol => User?.FindFirstValue(ClaimTypes.Role) ?? "";
    public string? Ip => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

/// <summary>
/// Si el token trae la marca de "debe cambiar contraseña", solo deja pasar el cambio de contraseña.
/// Así una contraseña temporal no sirve para ver datos clínicos.
/// </summary>
public class CambioPasswordObligatorioMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true
            && ctx.User.HasClaim(ClaimsPersonal.DebeCambiarPassword, "1")
            && !ctx.Request.Path.StartsWithSegments("/api/auth"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { status = 403, title = "Debes cambiar tu contraseña temporal antes de continuar." },
                options: null, contentType: "application/problem+json");
            return;
        }
        await next(ctx);
    }
}
