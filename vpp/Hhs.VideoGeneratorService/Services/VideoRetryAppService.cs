using Hhs.Shared.Configuration;
using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class VideoRetryAppService(
    VideoMongoContext context,
    IEventBus eventBus,
    IVideoProviderResolver videoProviderResolver,
    VideoRetrySettings retrySettings)
{
    private readonly VideoRetrySettings _retrySettings = retrySettings;

    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await RetryAudioRequestsAsync(now, cancellationToken);
        await RetryVideoRequestsAsync(now, cancellationToken);
    }

    private async Task RetryAudioRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var requests = await context.AudioRequests
            .Find(x =>
                x.Status == StatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now)
            .Limit(_retrySettings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                bool pollingRetry = false;

                if (request.CurrentStep == EventNames.AudioProviderRequestStarted)
                {
                    request.Status = StatusNames.AudioProviderRequestRetrying;

                    await eventBus.PublishAsync(new AudioProviderRequestStartedEto { CorrelationId = request.CorrelationId, AudioRequestId = request.Id, }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.AudioFileDownloadStarted)
                {
                    await eventBus.PublishAsync(new AudioFileDownloadStartedEto { CorrelationId = request.CorrelationId, AudioRequestId = request.Id, }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.AudioProviderPollingStarted)
                {
                    request.Status = StatusNames.AudioProviderPolling;
                    request.NextProviderPollAtUtc = DateTime.UtcNow;
                    pollingRetry = true;
                }
                else
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = $"Unsupported audio retry step: {request.CurrentStep}";
                    request.NextRetryAtUtc = null;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await context.AudioRequests.ReplaceOneAsync(
                        x => x.Id == request.Id,
                        request,
                        cancellationToken: cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = request.CurrentStep,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                if (!pollingRetry)
                {
                    request.Status = StatusNames.RetryEventPublished;
                }

                request.NextRetryAtUtc = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await context.AudioRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                request.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds);
                request.UpdatedAtUtc = DateTime.UtcNow;

                await context.AudioRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);

                throw;
            }
        }
    }

    private async Task RetryVideoRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var requests = await context.VideoRequests
            .Find(x =>
                x.Status == StatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now)
            .Limit(_retrySettings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                bool pollingRetry = false;

                if (request.CurrentStep == EventNames.VideoProviderRequestStarted)
                {
                    string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(request.ScopeKey);
                    var videoProvider = videoProviderResolver.Resolve(videoProviderKey);

                    var audioRequests = await context.AudioRequests
                        .Find(x => x.VideoRequestId == request.Id)
                        .ToListAsync(cancellationToken);

                    var orderedAudios = audioRequests
                        .OrderBy(x => x.SortOrder)
                        .ToList();

                    await eventBus.PublishAsync(new VideoProviderRequestStartedEto
                    {
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.Id,
                        AudioUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                            ? orderedAudios.Select(x => x.AudioStorageUrl!).ToList()
                            : []
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.VideoFileDownloadStarted)
                {
                    await eventBus.PublishAsync(new VideoFileDownloadStartedEto { CorrelationId = request.CorrelationId, VideoRequestId = request.Id, }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.VideoProviderPollingStarted)
                {
                    request.Status = StatusNames.VideoProviderPolling;
                    request.NextProviderPollAtUtc = DateTime.UtcNow;
                    pollingRetry = true;
                }
                else
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = $"Unsupported video retry step: {request.CurrentStep}";
                    request.NextRetryAtUtc = null;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await context.VideoRequests.ReplaceOneAsync(
                        x => x.Id == request.Id,
                        request,
                        cancellationToken: cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = request.CurrentStep,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                if (!pollingRetry)
                {
                    request.Status = StatusNames.RetryEventPublished;
                }

                request.NextRetryAtUtc = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await context.VideoRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                request.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds);
                request.UpdatedAtUtc = DateTime.UtcNow;

                await context.VideoRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);

                throw;
            }
        }
    }
}