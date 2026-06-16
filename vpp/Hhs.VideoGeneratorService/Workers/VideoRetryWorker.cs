using Hhs.VideoGeneratorService.Services;

namespace Hhs.VideoGeneratorService.Workers;

public sealed class VideoRetryWorker(
    IServiceProvider serviceProvider,
    ILogger<VideoRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Video retry worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                var appService = scope.ServiceProvider
                    .GetRequiredService<VideoRetryAppService>();

                await appService.RetryDueRequestsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video retry worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}