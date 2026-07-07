using System.Linq.Expressions;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoOperationRetryWorkerService(
    IServiceProvider provider,
    ILogger<VideoOperationRetryWorkerService> logger,
    IEventInboxMessageManager inboxManager,
    IVideoRequestRepository videoRequestRepository,
    IAudioRequestRepository audioRequestRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IVideoProviderResolver videoProviderResolver,
    VideoRetrySettings retrySettings) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await ResetStaleStartedInboxMessagesAsync(now, cancellationToken);
        await AdvanceReadyVideoRequestsToProviderStartAsync(cancellationToken);
        await RetryAudioRequestsAsync(now, cancellationToken);
        await RetryVideoRequestsAsync(now, cancellationToken);
    }

    /// <summary>
    /// Fan-in gate: a VideoRequest can only start its video-provider request once every sibling
    /// AudioRequest has reached a terminal state (AudioFileUploadCompleted or Failed). Instead of
    /// reacting to a single audio's completion event (which can be lost to a redelivery race —
    /// see the framework-level inbox reclaim fix), this re-derives readiness directly from the
    /// audios' current DB state on every tick, so it can never get permanently stuck waiting for
    /// an event that never arrives. The final claim (Started -&gt; VideoProviderRequestStarting) is
    /// atomic, so a duplicate tick (or a second worker instance) racing the same VideoRequest is
    /// always a safe no-op.
    /// </summary>
    private async Task AdvanceReadyVideoRequestsToProviderStartAsync(CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<VideoRequest>
        {
            Filter = x => x.Status == VideoStatusNames.Started,
            MaxResultCount = retrySettings.BatchSize
        };
        var candidates = await videoRequestRepository.GetListAsync(options, cancellationToken);

        foreach (var videoRequest in candidates)
        {
            try
            {
                var audioOptions = new ListQueryOptions<AudioRequest> { Filter = x => x.VideoRequestId == videoRequest.Id };
                var allAudios = await audioRequestRepository.GetListAsync(audioOptions, cancellationToken);

                if (allAudios.Count == 0)
                    continue; // audios not created yet — still inside StartVideoOperationAsync

                if (allAudios.Any(x => x.Status == AudioStatusNames.Failed))
                {
                    var failPredicate = (Expression<Func<VideoRequest, bool>>)(x =>
                        x.Id == videoRequest.Id && x.Status == VideoStatusNames.Started);
                    var failUpdate = Builders<VideoRequest>.Update
                        .Set(x => x.Status, VideoStatusNames.Failed)
                        .Set(x => x.CurrentStep, EventNames.AudioFileUploadCompleted)
                        .Set(x => x.LastError, "One or more audio requests failed.")
                        .Set(x => x.NextRetryAtUtc, (DateTime?)null);

                    var failClaimed = await videoRequestRepository.UpdateByExpressionAsync(failPredicate, u => failUpdate, cancellationToken: cancellationToken);
                    if (failClaimed == 0) continue;

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.AudioFileUploadCompleted,
                        reference: new { VideoRequestId = videoRequest.Id, videoRequest.RefContentId },
                        facility: EventNames.AudioFileUploadCompleted,
                        correlationId: videoRequest.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: videoRequest.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = videoRequest.RefContentId,
                            RefContentType = videoRequest.RefContentType,
                            Step = EventNames.AudioFileUploadCompleted,
                            ErrorMessage = "One or more audio requests failed.",
                            Retryable = false
                        }
                    );

                    continue;
                }

                if (allAudios.Any(x => x.Status != AudioStatusNames.AudioFileUploadCompleted))
                    continue; // still waiting on at least one sibling

                var providerKeyResult = await customerVpSettingRepository.GetVideoProviderKeyByScopeKeyAsync(videoRequest.ScopeKey, cancellationToken);
                if (!providerKeyResult.Key)
                {
                    logger.LogError("ADVANCE_VIDEO_PROVIDER_START_FAILED: unknown provider key. ScopeKey={ScopeKey}, VideoRequestId={VideoRequestId}", videoRequest.ScopeKey, videoRequest.Id);
                    continue;
                }

                var videoProvider = videoProviderResolver.Resolve(providerKeyResult.Value);
                var orderedAudios = allAudios.OrderBy(x => x.SortOrder).ToList();

                if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired &&
                    orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.AudioCdnUrl)))
                {
                    logger.LogError("ADVANCE_VIDEO_PROVIDER_START_FAILED: AudioCdnUrl missing. VideoRequestId={VideoRequestId}", videoRequest.Id);
                    continue;
                }

                var lockPredicate = (Expression<Func<VideoRequest, bool>>)(x =>
                    x.Id == videoRequest.Id && x.Status == VideoStatusNames.Started);

                var lockUpdate = Builders<VideoRequest>.Update
                    .Set(x => x.Status, VideoStatusNames.VideoProviderRequestStarting)
                    .Set(x => x.CurrentStep, EventNames.VideoProviderRequestStarted)
                    .Set(x => x.LastError, null);

                var lockResult = await videoRequestRepository.UpdateByExpressionAsync(lockPredicate, u => lockUpdate, cancellationToken: cancellationToken);
                if (lockResult == 0) continue; // another tick/instance already claimed this VideoRequest

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.VideoProviderRequestStarted,
                    reference: new { videoRequest.ScopeKey, videoRequest.RefContentId, VideoRequestId = videoRequest.Id, AudioCount = orderedAudios.Count },
                    facility: EventNames.VideoProviderRequestStarted,
                    correlationId: videoRequest.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: videoRequest.CorrelationId,
                    eventMessage: new VideoProviderRequestStartedEto
                    {
                        RefContentId = videoRequest.RefContentId,
                        RefContentType = videoRequest.RefContentType,
                        VideoRequestId = videoRequest.Id,
                        AudioCdnUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                            ? orderedAudios.Select(x => x.AudioCdnUrl!).ToList()
                            : []
                    }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADVANCE_VIDEO_PROVIDER_START_FAILED: VideoRequestId={VideoRequestId}", videoRequest.Id);
            }
        }
    }

    /// <summary>
    /// Resets EventInboxMessage records that are stuck in 'Started' status beyond the stale
    /// threshold back to 'Failed', so the next broker re-delivery can attempt processing.
    /// This handles the case where a handler crashed after inserting the inbox record but
    /// before calling CompleteAsync. Independent of the business-level (StepFailedEto) retry
    /// mechanism below, which only covers domain entities already past the inbox stage.
    /// </summary>
    private async Task ResetStaleStartedInboxMessagesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var staleThreshold = now.AddMinutes(-retrySettings.StaleInboxMessageThresholdMinutes);
        var updated = await inboxManager.ResetStaleStartedMessagesAsync(staleThreshold, cancellationToken);

        if (updated > 0)
        {
            logger.LogWarning(
                "Video-generator retry worker reset {Count} stale inbox message(s) from 'Started' to 'Failed'. " +
                "These will be re-processed on next broker re-delivery.",
                updated);
        }
    }

    private async Task RetryAudioRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<AudioRequest>
        {
            Filter = x =>
                x.Status == AudioStatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now,
            MaxResultCount = retrySettings.BatchSize
        };

        var requests = await audioRequestRepository.GetListAsync(options, cancellationToken);

        foreach (var request in requests)
        {
            var claimPredicate = (Expression<Func<AudioRequest, bool>>)(x => x.Id == request.Id && x.Status == AudioStatusNames.WaitingRetry);
            var claimUpdate = Builders<AudioRequest>.Update.Set(x => x.Status, AudioStatusNames.RetryEventPublished);
            var claimed = await audioRequestRepository.UpdateByExpressionAsync(claimPredicate, u => claimUpdate, cancellationToken: cancellationToken);
            if (claimed == 0) continue; // another tick/instance already claimed this AudioRequest

            try
            {
                bool pollingRetry = false;

                if (request.CurrentStep == EventNames.AudioProviderRequestStarted)
                {
                    request.Status = AudioStatusNames.AudioProviderRequestRetrying;

                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.AudioFileDownloadStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new AudioFileDownloadStartedEto { AudioRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.AudioFileUploadStarted)
                {
                    // Re-trigger from download so the local file is refreshed before re-uploading.
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new AudioFileDownloadStartedEto { AudioRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.AudioProviderPollingStarted)
                {
                    request.Status = AudioStatusNames.AudioProviderPolling;
                    request.NextProviderPollAtUtc = DateTime.UtcNow;
                    pollingRetry = true;
                }
                else
                {
                    request.Status = AudioStatusNames.Failed;
                    request.LastError = $"Unsupported audio retry step: {request.CurrentStep}";
                    request.NextRetryAtUtc = null;

                    var failPredicate = (Expression<Func<AudioRequest, bool>>)(x => x.Id == request.Id);
                    var failUpdate = Builders<AudioRequest>.Update
                        .Set(x => x.Status, request.Status)
                        .Set(x => x.LastError, request.LastError)
                        .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc);

                    await audioRequestRepository.UpdateByExpressionAsync(
                        failPredicate,
                        u => failUpdate,
                        cancellationToken: cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: request.CurrentStep,
                        reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, request.LastError },
                        facility: request.CurrentStep,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = request.CurrentStep,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: EventNames.RetryScheduled,
                    reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, FailedStep = request.CurrentStep },
                    facility: EventNames.RetryScheduled,
                    correlationId: request.CorrelationId,
                    exception: null
                ));

                if (!pollingRetry)
                {
                    request.Status = AudioStatusNames.RetryEventPublished;
                }

                request.NextRetryAtUtc = null;

                var updatePredicate = (Expression<Func<AudioRequest, bool>>)(x => x.Id == request.Id);
                var updateUpdate = Builders<AudioRequest>.Update
                    .Set(x => x.Status, request.Status)
                    .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc)
                    .Set(x => x.NextProviderPollAtUtc, request.NextProviderPollAtUtc);

                await audioRequestRepository.UpdateByExpressionAsync(
                    updatePredicate,
                    u => updateUpdate,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RETRY_AUDIO_REQUEST_FAILED: RequestId={RequestId}", request.Id);

                request.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds);

                var exceptionPredicate = (Expression<Func<AudioRequest, bool>>)(x => x.Id == request.Id);
                var exceptionUpdate = Builders<AudioRequest>.Update
                    .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc);

                await audioRequestRepository.UpdateByExpressionAsync(
                    exceptionPredicate,
                    u => exceptionUpdate,
                    cancellationToken: cancellationToken);

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: EventNames.RetryScheduled,
                    reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, FailedStep = request.CurrentStep, request.NextRetryAtUtc },
                    facility: EventNames.RetryScheduled,
                    correlationId: request.CorrelationId,
                    exception: ex
                ));
            }
        }
    }

    private async Task RetryVideoRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<VideoRequest>
        {
            Filter = x =>
                x.Status == VideoStatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now,
            MaxResultCount = retrySettings.BatchSize
        };

        var requests = await videoRequestRepository.GetListAsync(options, cancellationToken);

        foreach (var request in requests)
        {
            var claimPredicate = (Expression<Func<VideoRequest, bool>>)(x => x.Id == request.Id && x.Status == VideoStatusNames.WaitingRetry);
            var claimUpdate = Builders<VideoRequest>.Update.Set(x => x.Status, VideoStatusNames.RetryEventPublished);
            var claimed = await videoRequestRepository.UpdateByExpressionAsync(claimPredicate, u => claimUpdate, cancellationToken: cancellationToken);
            if (claimed == 0) continue; // another tick/instance already claimed this VideoRequest

            try
            {
                bool pollingRetry = false;

                if (request.CurrentStep == EventNames.VideoOperationStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new VideoRequestCreatedEto { RefContentId = request.RefContentId, RefContentType = request.RefContentType, VideoRequestId = request.Id }
                    );
                }
                else if (request.CurrentStep == EventNames.VideoProviderRequestStarted)
                {
                    var providerKeyResult = await customerVpSettingRepository.GetVideoProviderKeyByScopeKeyAsync(request.ScopeKey, cancellationToken);
                    if (!providerKeyResult.Key)
                    {
                        throw new InvalidOperationException($"Provider key value is unknown. Scope key: {request.ScopeKey}");
                    }

                    var videoProvider = videoProviderResolver.Resolve(providerKeyResult.Value);

                    var audioOptions = new ListQueryOptions<AudioRequest>
                    {
                        Filter = x => x.VideoRequestId == request.Id
                    };
                    var audioRequests = await audioRequestRepository.GetListAsync(audioOptions, cancellationToken);

                    var orderedAudios = audioRequests
                        .OrderBy(x => x.SortOrder)
                        .ToList();

                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new VideoProviderRequestStartedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            VideoRequestId = request.Id,
                            AudioCdnUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                                ? orderedAudios.Select(x => x.AudioCdnUrl!).ToList()
                                : []
                        }
                    );
                }
                else if (request.CurrentStep == EventNames.VideoFileDownloadStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new VideoFileDownloadStartedEto { VideoRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.VideoFileUploadStarted)
                {
                    // Re-trigger from download so the local file is refreshed before re-uploading.
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new VideoFileDownloadStartedEto { VideoRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.VideoFileUploadCompleted)
                {
                    // The file is already uploaded — just re-attempt publishing the final result.
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new VideoFileUploadCompletedEto { VideoRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.VideoProviderPollingStarted)
                {
                    request.Status = VideoStatusNames.VideoProviderPolling;
                    request.NextProviderPollAtUtc = DateTime.UtcNow;
                    pollingRetry = true;
                }
                else
                {
                    request.Status = VideoStatusNames.Failed;
                    request.LastError = $"Unsupported video retry step: {request.CurrentStep}";
                    request.NextRetryAtUtc = null;

                    var failPredicate = (Expression<Func<VideoRequest, bool>>)(x => x.Id == request.Id);
                    var failUpdate = Builders<VideoRequest>.Update
                        .Set(x => x.Status, request.Status)
                        .Set(x => x.LastError, request.LastError)
                        .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc);

                    await videoRequestRepository.UpdateByExpressionAsync(
                        failPredicate,
                        u => failUpdate,
                        cancellationToken: cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: request.CurrentStep,
                        reference: new { VideoRequestId = request.Id, request.RefContentId, request.LastError },
                        facility: request.CurrentStep,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = request.CurrentStep,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );
                    continue;
                }

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: EventNames.RetryScheduled,
                    reference: new { VideoRequestId = request.Id, request.RefContentId, FailedStep = request.CurrentStep },
                    facility: EventNames.RetryScheduled,
                    correlationId: request.CorrelationId,
                    exception: null
                ));

                if (!pollingRetry)
                {
                    request.Status = VideoStatusNames.RetryEventPublished;
                }

                request.NextRetryAtUtc = null;

                var updatePredicate = (Expression<Func<VideoRequest, bool>>)(x => x.Id == request.Id);
                var updateUpdate = Builders<VideoRequest>.Update
                    .Set(x => x.Status, request.Status)
                    .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc)
                    .Set(x => x.NextProviderPollAtUtc, request.NextProviderPollAtUtc);

                await videoRequestRepository.UpdateByExpressionAsync(
                    updatePredicate,
                    u => updateUpdate,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RETRY_VIDEO_REQUEST_FAILED: RequestId={RequestId}", request.Id);

                request.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds);

                var exceptionPredicate = (Expression<Func<VideoRequest, bool>>)(x => x.Id == request.Id);
                var exceptionUpdate = Builders<VideoRequest>.Update
                    .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc);

                await videoRequestRepository.UpdateByExpressionAsync(
                    exceptionPredicate,
                    u => exceptionUpdate,
                    cancellationToken: cancellationToken);

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: EventNames.RetryScheduled,
                    reference: new { VideoRequestId = request.Id, request.RefContentId, FailedStep = request.CurrentStep, request.NextRetryAtUtc },
                    facility: EventNames.RetryScheduled,
                    correlationId: request.CorrelationId,
                    exception: ex
                ));
            }
        }
    }
}