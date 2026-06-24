using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.MongoDb.Context;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoRetryAppService(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext context,
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
        var requests = await context.AudioRequests
            .Find(x =>
                x.Status == StatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now)
            .Limit(retrySettings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                bool pollingRetry = false;

                if (request.CurrentStep == EventNames.AudioProviderRequestStarted)
                {
                    request.Status = StatusNames.AudioProviderRequestRetrying;

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

                    await context.AudioRequests.ReplaceOneAsync(
                        x => x.Id == request.Id,
                        request,
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

                await context.AudioRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                request.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds);

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
            .Limit(retrySettings.BatchSize)
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

                    await context.VideoRequests.ReplaceOneAsync(
                        x => x.Id == request.Id,
                        request,
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

                await context.VideoRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                request.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds);

                await context.VideoRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);

                throw;
            }
        }
    }
}