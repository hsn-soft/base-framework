using Hhs.FeedRService.Application.Services;
using Hhs.FeedRService.Domain.Configuration;
using HsnSoft.Base.Data;

namespace Hhs.FeedRService.AdManager.Workers;

public class FeedRRetryWorker(
    IServiceProvider serviceProvider,
    FeedRRetrySettings settings,
    IDataFilter dataFilter,
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
                    using (dataFilter.Disable<IMultiTenant>())
                    {
                        using (dataFilter.Disable<IScopeSubscription>())
                        {
                            var retryService = scope.ServiceProvider.GetRequiredService<FeedROperationRetryWorkerService>();
                            await retryService.RetryDueRequestsAsync(stoppingToken);
                        }
                    }
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
