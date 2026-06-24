using Hhs.ContentService.Application.Services;
using Hhs.ContentService.Domain.Configuration;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Subscribe;
using Microsoft.Extensions.Options;

namespace Hhs.ContentService.Workers;

public sealed class ContentRetryWorker(
    IServiceProvider serviceProvider,
    IOptions<ContentRetrySettings> retrySettingsOptions,
    IDataFilter dataFilter,
    ILogger<ContentRetryWorker> logger) : BackgroundService
{
    private readonly ContentRetrySettings _retrySettings = retrySettingsOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Content retry worker started. Interval: {IntervalSeconds}s, Max Retries: {MaxRetryCount}",
            _retrySettings.RetryWorkerIntervalSeconds, _retrySettings.MaxRetryCount);

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
                            .GetRequiredService<ContentOperationRetryWorkerService>();

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
                logger.LogError(ex, "Content retry worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_retrySettings.RetryWorkerIntervalSeconds), stoppingToken);
        }
    }
}
