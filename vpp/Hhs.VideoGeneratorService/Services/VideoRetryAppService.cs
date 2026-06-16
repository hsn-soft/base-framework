using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class VideoRetryAppService(
    VideoMongoContext context,
    IEventBus eventBus,
    IVideoProviderResolver videoProviderResolver)
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
                x.Status == "WAITING_RETRY" &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var pollingRetry = false;

                if (request.CurrentStep == EventNames.AudioProviderRequestStarted)
                {
                    await eventBus.PublishAsync(new AudioProviderRequestStartedEvent
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
                    await eventBus.PublishAsync(new AudioFileDownloadStartedEvent
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
                    await eventBus.PublishAsync(new AudioFileUploadStartedEvent
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
                    throw new InvalidOperationException(
                        $"Unsupported audio retry step: {request.CurrentStep}");
                }

                if (!pollingRetry)
                {
                    request.Status = "RETRY_PUBLISHED";
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
                request.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
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
            .Limit(50)
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

                    await eventBus.PublishAsync(new VideoProviderRequestStartedEvent
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
                    await eventBus.PublishAsync(new VideoFileDownloadStartedEvent
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
                    await eventBus.PublishAsync(new VideoFileUploadStartedEvent
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
                    throw new InvalidOperationException(
                        $"Unsupported video retry step: {request.CurrentStep}");
                }

                if (!pollingRetry)
                {
                    request.Status = "RETRY_PUBLISHED";
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
                request.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
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