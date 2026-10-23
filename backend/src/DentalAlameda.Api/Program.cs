using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DentalAlameda.Api.Middleware;
using DentalAlameda.Api.Security;
using Microsoft.AspNetCore.HttpOverrides;
using DentalAlameda.Api.Workers;
using DentalAlameda.Application;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Infrastructure;
using DentalAlameda.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// En producción no se arranca con secretos de ejemplo
if (builder.Environment.IsProduction())
{
    var key = builder.Configuration["Jwt:Key"] ?? "";
    if (key.Contains("cambia", StringComparison.OrdinalIgnoreCase) || key.Length < 32)
        throw new InvalidOperationException("En producción Jwt__Key debe ser un secreto propio de al menos 32 caracteres (openssl rand -base64 48).");
    var cs = builder.Configuration.GetConnectionString("Default") ?? "";
    if (cs.Contains("Password=dental;", StringComparison.OrdinalIgnoreCase) || cs.EndsWith("Password=dental", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("En producción cambia la contraseña de la base de datos (DB_PASSWORD).");
}

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Detrás de Caddy/nginx: usar la IP real del cliente para auditoría y límites de intentos
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
    o.ForwardLimit = 2;
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("reservas", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        // Toda la clínica suele salir a internet por una sola IP: margen para varios empleados
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("password", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(5) }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Clínica Dental Alameda API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Token obtenido en POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddHostedService<NotificacionesWorker>();

var app = builder.Build();

// Migraciones y datos iniciales al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
        app.Configuration, app.Logger);
}

app.UseForwardedHeaders();
app.UseMiddleware<ErrorHandlingMiddleware>();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();   // después de autenticar: el límite de cambio de contraseña es por usuario
app.UseMiddleware<CambioPasswordObligatorioMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
