using Hhs.VideoGeneratorService.Application.Services;
using Hhs.VideoGeneratorService.Domain.Configuration;

namespace Hhs.VideoGeneratorService.Workers;

public sealed class AudioProviderPollingWorker(
    IServiceProvider serviceProvider,
    AudioPollingSettings pollingSettings,
    ILogger<AudioProviderPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Audio provider polling worker started. Interval: {IntervalSeconds}s, Max Attempts: {MaxAttempts}",
            pollingSettings.IntervalSeconds, pollingSettings.MaxAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var appService = scope.ServiceProvider
                    .GetRequiredService<AudioProviderPollingAppService>();

                await appService.PollDueAudioRequestsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Audio polling worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingSettings.IntervalSeconds), stoppingToken);
        }
    }
}