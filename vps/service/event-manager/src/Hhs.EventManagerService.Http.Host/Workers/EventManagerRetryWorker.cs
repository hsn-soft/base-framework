using Hhs.EventManagerService.Application.Services;
using Hhs.EventManagerService.Domain.Configuration;

namespace Hhs.EventManagerService.Workers;

public class EventManagerRetryWorker(
    IServiceProvider serviceProvider,
    EventManagerRetrySettings settings,
    ILogger<EventManagerRetryWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventManagerRetryWorker starting. Interval: {IntervalSeconds}s", settings.RetryWorkerIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var retryService = scope.ServiceProvider.GetRequiredService<EventOperationRetryWorkerService>();
                    await retryService.RetryDueRequestsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in EventManagerRetryWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.RetryWorkerIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("EventManagerRetryWorker stopped");
    }
}
