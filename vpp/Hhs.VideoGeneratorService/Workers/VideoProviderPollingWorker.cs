using Hhs.VideoGeneratorService.Services;

namespace Hhs.VideoGeneratorService.Workers;

public sealed class VideoProviderPollingWorker(
    IServiceProvider serviceProvider,
    ILogger<VideoProviderPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Video provider polling worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var appService = scope.ServiceProvider
                    .GetRequiredService<VideoProviderPollingAppService>();

                await appService.PollDueVideoRequestsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video polling worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}