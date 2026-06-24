using Hhs.TextNormalizerService.Application.Services;
using Hhs.TextNormalizerService.Domain.Configuration;

namespace Hhs.TextNormalizerService.Workers;

public sealed class NormalizerRetryWorker(
    IServiceProvider serviceProvider,
    NormalizerRetrySettings retrySettings,
    ILogger<NormalizerRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Normalizer retry worker started. Interval: {IntervalSeconds}s, Max Retries: {MaxRetryCount}",
            retrySettings.RetryWorkerIntervalSeconds, retrySettings.MaxRetryCount);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var appService = scope.ServiceProvider
                    .GetRequiredService<NormalizerOperationRetryWorkerService>();

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

            await Task.Delay(TimeSpan.FromSeconds(retrySettings.RetryWorkerIntervalSeconds), stoppingToken);
        }
    }
}