using Hhs.FeedRService.Application.Services;
using Hhs.FeedRService.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hhs.FeedRService.AdManager.Http.Host.Workers;

public class FeedRRetryWorker(
    IServiceProvider serviceProvider,
    FeedRRetrySettings settings,
    ILogger<FeedRRetryWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FeedRRetryWorker starting. Interval: {IntervalSeconds}s", settings.RetryWorkerIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var retryService = scope.ServiceProvider.GetRequiredService<FeedROperationRetryWorkerService>();
                    await retryService.RetryDueRequestsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in FeedRRetryWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.RetryWorkerIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("FeedRRetryWorker stopped");
    }
}
