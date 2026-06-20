using Hhs.Shared.Configuration;
using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Video;
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
                x.Status == "WAITING_RETRY" &&
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
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
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
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.VideoRequestId,
                        AudioRequestId = request.Id,
                        ProviderFileUrl = request.ProviderAudioFileUrl
                                          ?? throw new InvalidOperationException("ProviderAudioFileUrl is required.")
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.AudioFileUploadStarted)
                {
                    await eventBus.PublishAsync(new AudioFileUploadStartedEto
                    {
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.VideoRequestId,
                        AudioRequestId = request.Id,
                        LocalFilePath = request.LocalAudioFilePath
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
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
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
                x.Status == "WAITING_RETRY" &&
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
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.Id,
                        AudioUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                            ? orderedAudios.Select(x => x.AudioStorageUrl!).ToList()
                            : [],
                        AudioFilePaths = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired
                            ? orderedAudios.Select(x => x.LocalAudioFilePath!).ToList()
                            : []
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.VideoFileDownloadStarted)
                {
                    await eventBus.PublishAsync(new VideoFileDownloadStartedEto
                    {
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.Id,
                        ProviderFileUrl = request.ProviderVideoFileUrl
                                          ?? throw new InvalidOperationException("ProviderVideoFileUrl is required.")
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.VideoFileUploadStarted)
                {
                    await eventBus.PublishAsync(new VideoFileUploadStartedEto
                    {
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        VideoRequestId = request.Id,
                        LocalFilePath = request.LocalVideoFilePath
                                        ?? throw new InvalidOperationException("LocalVideoFilePath is required.")
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
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
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