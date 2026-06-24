using Hhs.IdentityService.Application.Services;
using Hhs.IdentityService.Domain.Configuration;

namespace Hhs.IdentityService.Workers;

public class IdentityRetryWorker(
    IServiceProvider serviceProvider,
    IdentityRetrySettings settings,
    ILogger<IdentityRetryWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IdentityRetryWorker starting. Interval: {IntervalSeconds}s", settings.RetryWorkerIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var retryService = scope.ServiceProvider.GetRequiredService<IdentityOperationRetryWorkerService>();
                    await retryService.RetryDueRequestsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in IdentityRetryWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.RetryWorkerIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("IdentityRetryWorker stopped");
    }
}
