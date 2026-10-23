using System.Text;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Infrastructure.Persistence;
using DentalAlameda.Infrastructure.Security;
using DentalAlameda.Infrastructure.WhatsApp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DentalAlameda.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Default")
                        ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IClock, BogotaClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
        services.Configure<JwtOptions>(config.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1)
            });
        services.AddAuthorization();

        // WhatsApp: "Twilio" envía de verdad; "Consola" (por defecto) solo escribe en el log
        services.Configure<ClinicaOptions>(config.GetSection("Clinica"));
        services.Configure<TwilioOptions>(config.GetSection("Twilio"));
        if (string.Equals(config["WhatsApp:Proveedor"], "Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var tw = config.GetSection("Twilio").Get<TwilioOptions>() ?? new();
            if (string.IsNullOrWhiteSpace(tw.AccountSid) || string.IsNullOrWhiteSpace(tw.AuthToken) || string.IsNullOrWhiteSpace(tw.From))
                throw new InvalidOperationException("WhatsApp:Proveedor=Twilio requiere Twilio:AccountSid, Twilio:AuthToken y Twilio:From.");
            services.AddHttpClient<INotificadorWhatsApp, TwilioWhatsAppNotificador>(c =>
            {
                c.BaseAddress = new Uri("https://api.twilio.com/");
                c.Timeout = TimeSpan.FromSeconds(15);
            });
        }
        else
        {
            services.AddScoped<INotificadorWhatsApp, ConsolaNotificador>();
        }
        return services;
    }
}
