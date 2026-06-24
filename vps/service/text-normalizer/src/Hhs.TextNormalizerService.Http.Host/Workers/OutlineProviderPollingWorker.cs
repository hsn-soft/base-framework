using Hhs.TextNormalizerService.Application.Services;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using HsnSoft.Base.Data;

namespace Hhs.TextNormalizerService.Workers;

public sealed class OutlineProviderPollingWorker(
    IServiceProvider serviceProvider,
    OutlinePollingSettings pollingSettings,
    IDataFilter dataFilter,
    ILogger<OutlineProviderPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outline provider polling worker started. Interval: {IntervalSeconds}s, Max Attempts: {MaxAttempts}",
            pollingSettings.IntervalSeconds, pollingSettings.MaxAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                using (dataFilter.Disable<IMultiTenant>())
                {
                    using (dataFilter.Disable<IScopeSubscription>())
                    {
                        var appService = scope.ServiceProvider.GetRequiredService<OutlineProviderPollingWorkerService>();

                        await appService.PollDueOutlineRequestsAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outline polling worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingSettings.IntervalSeconds), stoppingToken);
        }
    }
}