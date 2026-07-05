using System.Linq.Expressions;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoProviderPollingWorkerService(
    IServiceProvider provider,
    IVideoRequestRepository videoRequestRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IVideoProviderResolver videoProviderResolver,
    ILogger<VideoProviderPollingWorkerService> logger,
    VideoPollingSettings pollingSettings) : ApplicationServiceBase(provider)
{
    public async Task PollDueVideoRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var options = new ListQueryOptions<VideoRequest>
        {
            Filter = x =>
                x.Status == VideoStatusNames.VideoProviderPolling &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.VideoProviderTrackingId != null,
            MaxResultCount = 50
        };

        var requests = await videoRequestRepository.GetListAsync(options, cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var claimResult = await ClaimDueVideoPollingAsync(
                    request.Id,
                    now,
                    cancellationToken);

                if (claimResult == 0)
                    continue;

                if (request.ProviderPollingCount >= pollingSettings.MaxAttempts)
                {
                    request.Status = VideoStatusNames.Failed;
                    request.LastError = ErrorMessages.VideoProviderPollingTimeout;

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

                var providerKeyResult = await customerVpSettingRepository.GetVideoProviderKeyByScopeKeyAsync(request.ScopeKey, cancellationToken);
                if (!providerKeyResult.Key)
                {
                    throw new InvalidOperationException($"Provider key value is unknown. Scope key: {request.ScopeKey}");
                }

                var provider = videoProviderResolver.Resolve(providerKeyResult.Value);

                var status = await provider.GetStatusAsync(request.VideoProviderTrackingId!);

                if (status.IsFailed)
                {
                    request.Status = VideoStatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? "Video provider failed.";

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

                    await ReplaceVideoAsync(request, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                // set provider file url
                request.VideoProviderUrl = status.ProviderFileUrl;

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.Status = VideoStatusNames.VideoProviderCompleted;
                request.CurrentStep = EventNames.VideoProviderCompleted;
                request.LastError = null;

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

                if (request.ProviderPollingCount >= pollingSettings.MaxAttempts)
                {
                    request.Status = VideoStatusNames.Failed;
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

    private Task<long> ClaimDueVideoPollingAsync(
        Guid videoRequestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<VideoRequest, bool>>)(x =>
            x.Id == videoRequestId &&
            x.Status == VideoStatusNames.VideoProviderPolling &&
            x.NextProviderPollAtUtc != null &&
            x.NextProviderPollAtUtc <= now &&
            x.VideoProviderTrackingId != null);

        var update = Builders<VideoRequest>.Update
            .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds));

        return videoRequestRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken);
    }

    private Task ReplaceVideoAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<VideoRequest, bool>>)(x => x.Id == request.Id);
        var update = Builders<VideoRequest>.Update
            .Set(x => x.Status, request.Status)
            .Set(x => x.CurrentStep, request.CurrentStep)
            .Set(x => x.MediaInputJson, request.MediaInputJson)
            .Set(x => x.AudioProviderKey, request.AudioProviderKey)
            .Set(x => x.VideoProviderKey, request.VideoProviderKey)
            .Set(x => x.VideoProviderTrackingId, request.VideoProviderTrackingId)
            .Set(x => x.VideoProviderUrl, request.VideoProviderUrl)
            .Set(x => x.NextProviderPollAtUtc, request.NextProviderPollAtUtc)
            .Set(x => x.ProviderPollingCount, request.ProviderPollingCount)
            .Set(x => x.VideoLocalPath, request.VideoLocalPath)
            .Set(x => x.VideoCdnProviderKey, request.VideoCdnProviderKey)
            .Set(x => x.VideoCdnUrl, request.VideoCdnUrl)
            .Set(x => x.VideoStorageUrl, request.VideoStorageUrl)
            .Set(x => x.LastError, request.LastError);

        return videoRequestRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken);
    }
}