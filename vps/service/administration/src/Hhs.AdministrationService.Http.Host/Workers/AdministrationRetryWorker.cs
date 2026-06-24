using Hhs.AdministrationService.Application.Services;
using Hhs.AdministrationService.Domain.Configuration;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Subscribe;

namespace Hhs.AdministrationService.Workers;

public class AdministrationRetryWorker(
    IServiceProvider serviceProvider,
    AdministrationRetrySettings settings,
    IDataFilter dataFilter,
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
                    using (dataFilter.Disable<IMultiTenant>())
                    {
                        using (dataFilter.Disable<IScopeSubscription>())
                        {
                            var retryService = scope.ServiceProvider.GetRequiredService<AdministrationOperationRetryWorkerService>();
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
                logger.LogError(ex, "Error in AdministrationRetryWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.RetryWorkerIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("AdministrationRetryWorker stopped");
    }
}
