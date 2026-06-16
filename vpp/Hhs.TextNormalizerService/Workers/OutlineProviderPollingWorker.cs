using Hhs.TextNormalizerService.Services;

namespace Hhs.TextNormalizerService.Workers;

public sealed class OutlineProviderPollingWorker(
    IServiceProvider serviceProvider,
    ILogger<OutlineProviderPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outline provider polling worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var appService = scope.ServiceProvider
                    .GetRequiredService<OutlineProviderPollingAppService>();

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

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}