using Hhs.Shared.Configuration;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Providers.FileDownloader;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class VideoProviderPollingAppService(
    VideoMongoContext context,
    IVideoProviderResolver videoProviderResolver,
    IEventBus eventBus,
    ILogger<VideoProviderPollingAppService> logger,
    IFileDownloader fileDownloader,
    VideoFastExternalProviderSettings videoFastExternalSettings,
    VideoFastInternalProviderSettings videoFastInternalSettings,
    VideoQueueExternalProviderSettings videoQueueExternalSettings,
    VideoQueueInternalProviderSettings videoQueueInternalSettings,
    VideoPollingSettings pollingSettings)
{
    private readonly VideoPollingSettings _pollingSettings = pollingSettings;

    public async Task PollDueVideoRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var requests = await context.VideoRequests
            .Find(x =>
                x.Status == StatusNames.VideoProviderPolling &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.VideoProviderTrackingId != null)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var claimResult = await ClaimDueVideoPollingAsync(
                    request.Id,
                    now,
                    cancellationToken);

                if (claimResult.ModifiedCount == 0)
                    continue;

                if (request.ProviderPollingCount >= 60)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = ErrorMessages.VideoProviderPollingTimeout;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.VideoProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                var videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(request.ScopeKey);
                var provider = videoProviderResolver.Resolve(videoProviderKey);

                var status = await provider.GetStatusAsync(
                    request.VideoProviderTrackingId!,
                    cancellationToken);

                if (status.IsFailed)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? "Video provider failed.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.VideoProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                if (!status.IsCompleted)
                {
                    request.ProviderPollingCount++;
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_pollingSettings.IntervalSeconds);
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.Status = StatusNames.VideoProviderCompleted;
                request.CurrentStep = EventNames.VideoProviderCompleted;
                request.LastError = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(request, cancellationToken);

                // Publish provider completed event - handler will trigger download cascade
                await eventBus.PublishAsync(new VideoProviderCompletedEto
                {
                    RefContentId = request.RefContentId,
                    RefContentType = request.RefContentType,
                    CorrelationId = request.CorrelationId,
                    VideoRequestId = request.Id,
                    ProviderFileUrl = status.ProviderFileUrl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                request.ProviderPollingCount++;
                request.LastError = ex.Message;
                request.UpdatedAtUtc = DateTime.UtcNow;

                if (request.ProviderPollingCount >= 60)
                {
                    request.Status = StatusNames.Failed;
                    request.NextProviderPollAtUtc = null;

                    await ReplaceVideoAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.VideoProviderPollingStarted,
                        ErrorMessage = ex.Message,
                        Retryable = false
                    }, cancellationToken);
                }
                else
                {
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_pollingSettings.BackoffIntervalSeconds);
                    await ReplaceVideoAsync(request, cancellationToken);
                }

                logger.LogError(
                    ex,
                    "Video provider polling failed. VideoRequestId: {VideoRequestId}",
                    request.Id);
            }
        }
    }

    private Task<UpdateResult> ClaimDueVideoPollingAsync(
        Guid videoRequestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return context.VideoRequests.UpdateOneAsync(
            x =>
                x.Id == videoRequestId &&
                x.Status == StatusNames.VideoProviderPolling &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.VideoProviderTrackingId != null,
            Builders<VideoRequest>.Update
                .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(_pollingSettings.ErrorRescheduleDelaySeconds))
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    private Task ReplaceVideoAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        return context.VideoRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }
}