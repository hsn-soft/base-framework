using Hhs.TextNormalizerService.Services;

namespace Hhs.TextNormalizerService.Workers;

public sealed class NormalizerRetryWorker(
    IServiceProvider serviceProvider,
    ILogger<NormalizerRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Normalizer retry worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var appService = scope.ServiceProvider
                    .GetRequiredService<NormalizerRetryAppService>();

                await appService.RetryDueRequestsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Normalizer retry worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}