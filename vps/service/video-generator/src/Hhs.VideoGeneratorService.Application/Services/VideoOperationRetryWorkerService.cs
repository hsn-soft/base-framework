using System.Linq.Expressions;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoOperationRetryWorkerService(
    IServiceProvider provider,
    ILogger<VideoOperationRetryWorkerService> logger,
    IVideoRequestRepository videoRequestRepository,
    IAudioRequestRepository audioRequestRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IVideoProviderResolver videoProviderResolver,
    VideoRetrySettings retrySettings) : ApplicationServiceBase(provider)
{
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
        var options = new ListQueryOptions<AudioRequest>
        {
            Filter = x =>
                x.Status == StatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now,
            MaxResultCount = retrySettings.BatchSize
        };

        var requests = await audioRequestRepository.GetListAsync(options, cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                bool pollingRetry = false;

                if (request.CurrentStep == EventNames.AudioProviderRequestStarted)
                {
                    request.Status = AudioStatusNames.AudioProviderRequestRetrying;

                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.AudioFileDownloadStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new AudioFileDownloadStartedEto { AudioRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.AudioFileUploadStarted)
                {
                    // Re-trigger from download so the local file is refreshed before re-uploading.
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
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
                    request.Status = StatusNames.Failed;
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

                if (!pollingRetry)
                {
                    request.Status = StatusNames.RetryEventPublished;
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
                x.Status == StatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now,
            MaxResultCount = retrySettings.BatchSize
        };

        var requests = await videoRequestRepository.GetListAsync(options, cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                bool pollingRetry = false;

                if (request.CurrentStep == EventNames.VideoProviderRequestStarted)
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
                        eventMessage: new VideoProviderRequestStartedEto
                        {
                            VideoRequestId = request.Id,
                            AudioUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                                ? orderedAudios.Select(x => x.AudioStorageUrl!).ToList()
                                : []
                        }
                    );
                }
                else if (request.CurrentStep == EventNames.VideoFileDownloadStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new VideoFileDownloadStartedEto { VideoRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.VideoFileUploadStarted)
                {
                    // Re-trigger from download so the local file is refreshed before re-uploading.
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new VideoFileDownloadStartedEto { VideoRequestId = request.Id, }
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
                    request.Status = StatusNames.Failed;
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

                if (!pollingRetry)
                {
                    request.Status = StatusNames.RetryEventPublished;
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
            }
        }
    }
}