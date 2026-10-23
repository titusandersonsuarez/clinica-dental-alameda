using DentalAlameda.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DentalAlameda.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services
        .AddScoped<CatalogoService>()
        .AddScoped<AgendaService>()
        .AddScoped<CitasAdminService>()
        .AddScoped<PacientesService>()
        .AddScoped<AuthService>()
        .AddScoped<NotificacionesService>()
        .AddScoped<UsuariosService>()
        .AddScoped<AuditoriaService>();
}
