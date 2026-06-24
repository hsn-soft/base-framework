using Hhs.AdministrationService.Application.Services;
using Hhs.AdministrationService.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hhs.AdministrationService.Http.Host.Workers;

public class AdministrationRetryWorker(
    IServiceProvider serviceProvider,
    AdministrationRetrySettings settings,
    ILogger<AdministrationRetryWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AdministrationRetryWorker starting. Interval: {IntervalSeconds}s", settings.RetryWorkerIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var retryService = scope.ServiceProvider.GetRequiredService<AdministrationOperationRetryWorkerService>();
                    await retryService.RetryDueRequestsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AdministrationRetryWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.RetryWorkerIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("AdministrationRetryWorker stopped");
    }
}
