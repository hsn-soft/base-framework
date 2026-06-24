using Hhs.VideoGeneratorService.Application.Services;
using Hhs.VideoGeneratorService.Domain.Configuration;
using HsnSoft.Base.Data;

namespace Hhs.VideoGeneratorService.Workers;

public sealed class VideoRetryWorker(
    IServiceProvider serviceProvider,
    VideoRetrySettings retrySettings,
    IDataFilter dataFilter,
    ILogger<VideoRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Video retry worker started. Interval: {IntervalSeconds}s, Max Retries: {MaxRetryCount}",
            retrySettings.RetryWorkerIntervalSeconds, retrySettings.MaxRetryCount);

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
                            .GetRequiredService<VideoOperationRetryWorkerService>();

                        await appService.RetryDueRequestsAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video retry worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(retrySettings.RetryWorkerIntervalSeconds), stoppingToken);
        }
    }
}