using Hhs.VideoGeneratorService.Application.Services;
using Hhs.VideoGeneratorService.Domain.Configuration;
using HsnSoft.Base.Data;

namespace Hhs.VideoGeneratorService.Workers;

public sealed class VideoProviderPollingWorker(
    IServiceProvider serviceProvider,
    VideoPollingSettings pollingSettings,
    IDataFilter dataFilter,
    ILogger<VideoProviderPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Video provider polling worker started. Interval: {IntervalSeconds}s, Max Attempts: {MaxAttempts}",
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
                        var appService = scope.ServiceProvider
                            .GetRequiredService<VideoProviderPollingWorkerService>();

                        await appService.PollDueVideoRequestsAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video polling worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingSettings.IntervalSeconds), stoppingToken);
        }
    }
}