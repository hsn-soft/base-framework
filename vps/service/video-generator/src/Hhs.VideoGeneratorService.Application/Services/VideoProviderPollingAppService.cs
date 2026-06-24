using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Configuration;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoProviderPollingAppService(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext context,
    IVideoProviderResolver videoProviderResolver,
    ILogger<VideoProviderPollingAppService> logger,
    VideoPollingSettings pollingSettings) : ApplicationServiceBase(provider)
{
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

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = EventNames.VideoProviderPollingStarted,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(request.ScopeKey);
                var provider = videoProviderResolver.Resolve(videoProviderKey);

                var status = await provider.GetStatusAsync(request.VideoProviderTrackingId!);

                if (status.IsFailed)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? "Video provider failed.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = EventNames.VideoProviderPollingStarted,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                if (!status.IsCompleted)
                {
                    request.ProviderPollingCount++;
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.IntervalSeconds);
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                // set provider file url
                request.VideoProviderUrl = status.ProviderFileUrl;

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.Status = StatusNames.VideoProviderCompleted;
                request.CurrentStep = EventNames.VideoProviderCompleted;
                request.LastError = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(request, cancellationToken);

                // Publish provider completed event - handler will trigger download cascade
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new VideoProviderCompletedEto { VideoRequestId = request.Id }
                );
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

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = EventNames.VideoProviderPollingStarted,
                            ErrorMessage = ex.Message,
                            Retryable = false
                        }
                    );
                }
                else
                {
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.BackoffIntervalSeconds);
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
                .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds))
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