using DentalAlameda.Application.Services;

namespace DentalAlameda.Api.Workers;

/// <summary>Procesa la cola de avisos de WhatsApp cada 15 segundos.</summary>
public class NotificacionesWorker(IServiceScopeFactory scopes, ILogger<NotificacionesWorker> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<NotificacionesService>();
                // Vacía la cola en lotes antes de esperar el siguiente ciclo
                while (await svc.ProcesarPendientesAsync(20, stoppingToken) == 20 && !stoppingToken.IsCancellationRequested) { }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Error procesando la cola de WhatsApp");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
