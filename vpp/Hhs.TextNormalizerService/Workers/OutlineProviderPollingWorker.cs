using Hhs.TextNormalizerService.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Services;

namespace Hhs.TextNormalizerService.Workers;

public sealed class OutlineProviderPollingWorker(
    IServiceProvider serviceProvider,
    OutlinePollingSettings pollingSettings,
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

                var appService = scope.ServiceProvider.GetRequiredService<OutlineProviderPollingAppService>();

                await appService.PollDueOutlineRequestsAsync(stoppingToken);
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