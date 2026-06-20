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
                var pollingRetry = false;

                if (request.CurrentStep == EventNames.AudioProviderRequestStarted)
                {
                    request.Status = "AUDIO_PROVIDER_REQUEST_RETRYING";

                    await eventBus.PublishAsync(new AudioProviderRequestStartedEto
                    {
                        RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.VideoRequestId,
                        AudioRequestId = request.Id,
                        SortOrder = request.SortOrder,
                        InputText = request.InputText
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.AudioFileDownloadStarted)
                {
                    await eventBus.PublishAsync(new AudioFileDownloadStartedEto
                    {
                        RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.VideoRequestId,
                        AudioRequestId = request.Id,
                        ProviderFileUrl = request.AudioProviderUrl
                                          ?? throw new InvalidOperationException("ProviderAudioFileUrl is required.")
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.AudioFileUploadStarted)
                {
                    await eventBus.PublishAsync(new AudioFileUploadStartedEto
                    {
                        RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.VideoRequestId,
                        AudioRequestId = request.Id,
                        LocalFilePath = request.AudioLocalPath
                                        ?? throw new InvalidOperationException("LocalAudioFilePath is required.")
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.AudioProviderPollingStarted)
                {
                    request.Status = "AUDIO_PROVIDER_POLLING";
                    request.NextProviderPollAtUtc = DateTime.UtcNow;
                    pollingRetry = true;
                }
                else
                {
                    request.Status = "FAILED";
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
                    request.Status = "RETRY_EVENT_PUBLISHED";
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
                var pollingRetry = false;

                if (request.CurrentStep == EventNames.VideoProviderRequestStarted)
                {
                    var videoProvider = videoProviderResolver.Resolve(request.VideoProviderKey);

                    var audioRequests = await context.AudioRequests
                        .Find(x => x.VideoRequestId == request.Id)
                        .ToListAsync(cancellationToken);

                    var orderedAudios = audioRequests
                        .OrderBy(x => x.SortOrder)
                        .ToList();

                    await eventBus.PublishAsync(new VideoProviderRequestStartedEto
                    {
                        RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.Id,
                        AudioUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                            ? orderedAudios.Select(x => x.AudioStorageUrl!).ToList()
                            : [],
                        AudioFilePaths = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired
                            ? orderedAudios.Select(x => x.AudioLocalPath!).ToList()
                            : []
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.VideoFileDownloadStarted)
                {
                    await eventBus.PublishAsync(new VideoFileDownloadStartedEto
                    {
                        RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.Id,
                        ProviderFileUrl = request.VideoProviderUrl
                                          ?? throw new InvalidOperationException("VideoProviderUrl is required.")
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.VideoFileUploadStarted)
                {
                    await eventBus.PublishAsync(new VideoFileUploadStartedEto
                    {
                        RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.Id,
                        LocalFilePath = request.VideoLocalPath
                                        ?? throw new InvalidOperationException("VideoLocalPath is required.")
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.VideoProviderPollingStarted)
                {
                    request.Status = "VIDEO_PROVIDER_POLLING";
                    request.NextProviderPollAtUtc = DateTime.UtcNow;
                    pollingRetry = true;
                }
                else
                {
                    request.Status = "FAILED";
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
                    request.Status = "RETRY_EVENT_PUBLISHED";
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