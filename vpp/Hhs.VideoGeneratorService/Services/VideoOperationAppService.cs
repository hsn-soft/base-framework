using System.Text.Json;
using Hhs.Shared.Configuration;
using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Retry;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Audio;
using Hhs.VideoGeneratorService.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Providers.Audio;
using Hhs.VideoGeneratorService.Providers.FileDownloader;
using Hhs.VideoGeneratorService.Providers.Video;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class VideoOperationAppService(
    VideoMongoContext context,
    IFileDownloader fileDownloader,
    IStorageService storageService,
    IEventBus eventBus,
    IVideoProviderResolver videoProviderResolver,
    IAudioProviderResolver audioProviderResolver,
    HttpClient httpClient,
    ILogger<VideoOperationAppService> logger,
    RetryDelayCalculator retryDelayCalculator,
    AudioFastProviderSettings audioFastSettings,
    AudioQueueProviderSettings audioQueueSettings,
    VideoFastExternalProviderSettings videoFastExternalSettings,
    VideoFastInternalProviderSettings videoFastInternalSettings,
    VideoQueueExternalProviderSettings videoQueueExternalSettings,
    VideoQueueInternalProviderSettings videoQueueInternalSettings,
    StorageProviderSettings storageSettings,
    VideoPollingSettings videoPollingSettings)
{
    private readonly RetryDelayCalculator _retryDelayCalculator = retryDelayCalculator;
    private readonly VideoPollingSettings _videoPollingSettings = videoPollingSettings;
public async Task CreateVideoRequestAsync(
    VideoGenerationApprovedEto @event,
    CancellationToken cancellationToken)
{
    try
    {
        var existing = await context.VideoRequests
            .Find(x => x.SourceEventId == @event.EventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            var provider = videoProviderResolver.Resolve(existing.VideoProviderKey);
            var existingExternalAudioRequired =
                provider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired ||
                provider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired;

            await eventBus.PublishAsync(new VideoRequestCreatedEto
            {
                RefContentId = existing.RefContentId,
                RefContentType = existing.RefContentType,
                CorrelationId = existing.CorrelationId,
                VideoRequestId = existing.Id,
                IsAnalysis = existing.RefContentType == ContentType.AnalysisContent,
                ExternalAudioRequired = existingExternalAudioRequired
            }, cancellationToken);

            return;
        }

        var videoProvider = videoProviderResolver.Resolve(@event.VideoProviderKey);

        var videoRequestId = Guid.NewGuid();

        var videoRequest = new VideoRequest
        {
            Id = videoRequestId,
            SourceEventId = @event.EventId,
            CorrelationId = @event.CorrelationId,
            RefContentId = @event.RefContentId,
            RefContentType = @event.RefContentType,
            Status = StatusNames.Created,
            CurrentStep = EventNames.VideoRequestCreated,
            MediaInputJson = @event.VideoInputJson,
            VideoProviderKey = @event.VideoProviderKey,
            AudioProviderKey = @event.AudioProviderKey,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await context.VideoRequests.InsertOneAsync(videoRequest, cancellationToken: cancellationToken);

        var externalAudioRequired =
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired ||
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired;

        await eventBus.PublishAsync(new VideoRequestCreatedEto
        {
            RefContentId = @event.RefContentId,
            RefContentType = @event.RefContentType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = videoRequestId,
            IsAnalysis = @event.RefContentType == ContentType.AnalysisContent,
            ExternalAudioRequired = externalAudioRequired
        }, cancellationToken);
    }
    catch (Exception ex)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(ex,
                "CreateVideoRequestAsync failed for event {EventId}. VideoProviderKey={VideoProviderKey}, RefContentId={RefContentId}, RefContentType={RefContentType}",
                @event.EventId,
                @event.VideoProviderKey,
                @event.RefContentId,
                @event.RefContentType);
        }
        throw;
    }
}

    public async Task StartVideoOperationAsync(VideoRequestCreatedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);
        var videoProvider = videoProviderResolver.Resolve(videoRequest.VideoProviderKey);
        var externalAudioRequired =
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired ||
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired;

        videoRequest.Status = StatusNames.Started;
        videoRequest.CurrentStep = EventNames.VideoOperationStarted;
        videoRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(videoRequest, cancellationToken);

        await eventBus.PublishAsync(new VideoOperationStartedEto
        {
            RefContentId = videoRequest.RefContentId,
            RefContentType = videoRequest.RefContentType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = videoRequest.Id,
            ExternalAudioRequired = externalAudioRequired
        }, cancellationToken);
    }

    public async Task HandleVideoOperationStartedAsync(
        VideoOperationStartedEto @event,
        CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);
        var videoProvider = videoProviderResolver.Resolve(videoRequest.VideoProviderKey);

        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.ProviderCreatesAudio ||
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.NoAudio)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Skipping audio operations for video request {VideoRequestId}. " +
                    "Video provider '{VideoProviderKey}' has AudioInputMode={AudioInputMode}. " +
                    "Selected AudioProviderKey was: {AudioProviderKey}",
                    videoRequest.Id,
                    videoRequest.VideoProviderKey,
                    videoProvider.Capabilities.AudioInputMode,
                    videoRequest.AudioProviderKey ?? "null");
            }

            await eventBus.PublishAsync(new VideoProviderRequestStartedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                AudioUrls = []
            }, cancellationToken);

            return;
        }

        var audioItems = ExtractAudioItems(videoRequest.MediaInputJson);

        foreach (var item in audioItems)
        {
            var existingAudio = await context.AudioRequests
                .Find(x => x.VideoRequestId == videoRequest.Id && x.SortOrder == item.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAudio is not null)
            {
                await eventBus.PublishAsync(new AudioProviderRequestStartedEto
                {
                    RefContentId = existingAudio.RefContentId,
                RefContentType = existingAudio.RefContentType,
                    CorrelationId = existingAudio.CorrelationId,
                    VideoRequestId = existingAudio.VideoRequestId,
                    AudioRequestId = existingAudio.Id,
                    SortOrder = existingAudio.SortOrder,
                    InputText = existingAudio.InputText
                }, cancellationToken);

                continue;
            }



            var audioRequestId = Guid.NewGuid();

            var audioRequest = new AudioRequest
            {
                Id = audioRequestId,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                SortOrder = item.SortOrder,
                InputText = item.Text,
                AudioProviderKey = videoRequest.AudioProviderKey
                                   ?? throw new InvalidOperationException("AudioProviderKey is required."),
                Status = StatusNames.AudioRequestCreated,
                CurrentStep = EventNames.AudioRequestCreated,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await context.AudioRequests.InsertOneAsync(audioRequest, cancellationToken: cancellationToken);

            await eventBus.PublishAsync(new AudioProviderRequestStartedEto
            {
                RefContentId = audioRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                AudioRequestId = audioRequest.Id,
                SortOrder = audioRequest.SortOrder,
                InputText = audioRequest.InputText
            }, cancellationToken);
        }
    }

    public async Task StartAudioProviderRequestAsync(
        AudioProviderRequestStartedEto @event,
        CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);
        var audioProviderKey = SubscriptionScopeRegistry.GetAudioProviderKey(audioRequest.ScopeKey);
        var provider = audioProviderResolver.Resolve(audioProviderKey);

        try
        {
            audioRequest.Status = StatusNames.AudioProviderRequestStarted;
            audioRequest.CurrentStep = EventNames.AudioProviderRequestStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var response = await provider.CreateAsync(new AudioCreateRequest { InputText = audioRequest.InputText }, cancellationToken);

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                // Download file from provider and upload to mock storage
                var localFileName = $"local_audio_{audioRequest.Id:N}.mp3";
                var mockStorageUrl = await DownloadAndUploadToStorageAsync(
                    response.ProviderFileUrl,
                    localFileName,
                    cancellationToken);

                audioRequest.AudioProviderUrl = mockStorageUrl;
                audioRequest.Status = StatusNames.AudioProviderCompleted;
                audioRequest.CurrentStep = EventNames.AudioProviderCompleted;
                audioRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceAudioAsync(audioRequest, cancellationToken);

                await eventBus.PublishAsync(new AudioProviderCompletedEto
                {
                    RefContentId = audioRequest.RefContentId,
                RefContentType = @event.RefContentType,
                    CorrelationId = @event.CorrelationId,
                    VideoRequestId = audioRequest.VideoRequestId,
                    AudioRequestId = audioRequest.Id,
                    ProviderFileUrl = mockStorageUrl
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Audio provider track id is required.");

            audioRequest.AudioProviderTrackingId = response.ProviderTrackId;
            audioRequest.Status = StatusNames.AudioProviderPolling;
            audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_videoPollingSettings.ErrorRescheduleDelaySeconds);
            audioRequest.ProviderPollingCount = 0;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await eventBus.PublishAsync(new AudioProviderPollingStartedEto
            {
                RefContentId = audioRequest.RefContentId,
                RefContentType = @event.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = audioRequest.VideoRequestId,
                AudioRequestId = audioRequest.Id,
                ProviderKey = audioRequest.AudioProviderKey,
                ProviderTrackId = response.ProviderTrackId!
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioProviderRequestStarted,
                ex,
                cancellationToken);

            return;
        }
    }


    public async Task HandleAudioProviderCompletedAsync(
        AudioProviderCompletedEto @event,
        CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(new AudioFileDownloadStartedEto
        {
            RefContentId = @event.RefContentId,
                RefContentType = @event.RefContentType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = @event.VideoRequestId,
            AudioRequestId = @event.AudioRequestId,
            ProviderFileUrl = @event.ProviderFileUrl
        }, cancellationToken);
    }

    public async Task DownloadAudioFileAsync(
        AudioFileDownloadStartedEto @event,
        CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = StatusNames.Downloading;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var localPath = await fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp3", $"audio_{audioRequest.Id:N}", cancellationToken);

            audioRequest.AudioLocalPath = localPath;
            audioRequest.Status = StatusNames.Downloaded;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await eventBus.PublishAsync(new AudioFileDownloadCompletedEto
            {
                RefContentId = audioRequest.RefContentId,
                RefContentType = @event.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = audioRequest.VideoRequestId,
                AudioRequestId = audioRequest.Id,
                LocalFilePath = localPath
            }, cancellationToken);

            await eventBus.PublishAsync(new AudioFileUploadStartedEto
            {
                RefContentId = audioRequest.RefContentId,
                RefContentType = @event.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = audioRequest.VideoRequestId,
                AudioRequestId = audioRequest.Id,
                LocalFilePath = localPath
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioFileDownloadStarted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task UploadAudioFileAsync(
        AudioFileUploadStartedEto @event,
        CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = StatusNames.Uploading;
            audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            // Upload file to CDN using selected provider
            var cdnProviderKey = ProviderDefaults.DefaultCdnProvider; // Decision from settings, not entity

            await using var fileStream = File.OpenRead(@event.LocalFilePath);
            var fileName = Path.GetFileName(@event.LocalFilePath);

            var (storageUrl, cdnUrl) = await storageService.UploadAsync(
                cdnProviderKey,
                fileStream,
                fileName,
                cancellationToken);

            audioRequest.AudioStorageUrl = storageUrl;
            audioRequest.AudioCdnUrl = cdnUrl;
            audioRequest.AudioCdnProviderKey = cdnProviderKey;
            audioRequest.Status = StatusNames.Uploaded;
            audioRequest.CurrentStep = EventNames.AudioFileUploadCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await eventBus.PublishAsync(new AudioFileUploadCompletedEto
            {
                RefContentId = audioRequest.RefContentId,
                RefContentType = @event.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = audioRequest.VideoRequestId,
                AudioRequestId = audioRequest.Id,
                StorageUrl = storageUrl
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioFileUploadStarted,
                ex,
                cancellationToken);

            return;
        }
    }

public async Task HandleAudioUploadCompletedAsync(
    AudioFileUploadCompletedEto @event,
    CancellationToken cancellationToken)
{
    var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

    try
    {
        var videoProvider = videoProviderResolver.Resolve(videoRequest.VideoProviderKey);

        var allAudios = await context.AudioRequests
            .Find(x => x.VideoRequestId == @event.VideoRequestId)
            .ToListAsync(cancellationToken);

        if (allAudios.Any(x => x.Status != StatusNames.Uploaded))
            return;

        var orderedAudios = allAudios.OrderBy(x => x.SortOrder).ToList();

        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired &&
            orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.AudioStorageUrl)))
        {
            throw new InvalidOperationException("AudioStorageUrl is required for video provider.");
        }

        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired &&
            orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.AudioLocalPath)))
        {
            throw new InvalidOperationException("LocalAudioFilePath is required for video provider.");
        }

        var lockResult = await context.VideoRequests.UpdateOneAsync(
            x => x.Id == @event.VideoRequestId &&
                 x.Status != StatusNames.VideoProviderRequestStarting &&
                 x.Status != StatusNames.VideoProviderRequestStarted &&
                 x.Status != StatusNames.VideoProviderPolling &&
                 x.Status != StatusNames.VideoProviderCompleted &&
                 x.Status != StatusNames.VideoDownloading &&
                 x.Status != StatusNames.VideoUploading &&
                 x.Status != StatusNames.Completed &&
                 x.Status != StatusNames.Failed,
            Builders<VideoRequest>.Update
                .Set(x => x.Status, StatusNames.VideoProviderRequestStarting)
                .Set(x => x.CurrentStep, EventNames.VideoProviderRequestStarted)
                .Set(x => x.LastError, null)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        if (lockResult.ModifiedCount == 0)
            return;

        videoRequest.Status = StatusNames.VideoProviderRequestStarting;

        await eventBus.PublishAsync(new VideoProviderRequestStartedEto
        {
            RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = videoRequest.Id,
            AudioUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                ? orderedAudios.Select(x => x.AudioStorageUrl!).ToList()
                : [],
            AudioFilePaths = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired
                ? orderedAudios.Select(x => x.AudioLocalPath!).ToList()
                : []
        }, cancellationToken);
    }
    catch (Exception ex)
    {
        await HandleVideoExceptionAsync(
            videoRequest,
            EventNames.VideoProviderRequestStarted,
            ex,
            cancellationToken);
    }
}
    public async Task StartVideoProviderRequestAsync(
        VideoProviderRequestStartedEto @event,
        CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);
        var provider = videoProviderResolver.Resolve(videoRequest.VideoProviderKey);

        try
        {
            videoRequest.Status = StatusNames.VideoProviderRequestStarted;
            videoRequest.CurrentStep = EventNames.VideoProviderRequestStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var response = await provider.CreateAsync(new VideoCreateRequest { VideoInputJson = videoRequest.MediaInputJson, AudioUrls = @event.AudioUrls, AudioFilePaths = @event.AudioFilePaths }, cancellationToken);

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                // Download file from provider and upload to mock storage
                var localFileName = $"local_video_{videoRequest.Id:N}.mp4";
                var mockStorageUrl = await DownloadAndUploadToStorageAsync(
                    response.ProviderFileUrl,
                    localFileName,
                    cancellationToken);

                videoRequest.VideoProviderUrl = mockStorageUrl;
                videoRequest.Status = StatusNames.VideoProviderCompleted;
                videoRequest.CurrentStep = EventNames.VideoProviderCompleted;
                videoRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(videoRequest, cancellationToken);

                await eventBus.PublishAsync(new VideoProviderCompletedEto
                {
                    RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                    CorrelationId = @event.CorrelationId,
                    VideoRequestId = videoRequest.Id,
                    ProviderFileUrl = mockStorageUrl
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Video provider track id is required.");

            videoRequest.VideoProviderTrackingId = response.ProviderTrackId;
            videoRequest.Status = StatusNames.VideoProviderPolling;
            videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_videoPollingSettings.ErrorRescheduleDelaySeconds);
            videoRequest.ProviderPollingCount = 0;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoProviderPollingStartedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                ProviderKey = videoRequest.VideoProviderKey,
                ProviderTrackId = response.ProviderTrackId!
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoProviderRequestStarted,
                ex,
                cancellationToken);

            return;
        }
    }


    public async Task HandleVideoProviderCompletedAsync(
        VideoProviderCompletedEto @event,
        CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(new VideoFileDownloadStartedEto
        {
            RefContentId = @event.RefContentId,
                RefContentType = @event.RefContentType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = @event.VideoRequestId,
            ProviderFileUrl = @event.ProviderFileUrl
        }, cancellationToken);
    }

    public async Task DownloadVideoFileAsync(
        VideoFileDownloadStartedEto @event,
        CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = StatusNames.VideoDownloading;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var localPath = await fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp4", $"video_{videoRequest.Id:N}", cancellationToken);

            videoRequest.VideoLocalPath = localPath;
            videoRequest.Status = StatusNames.VideoDownloaded;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadCompleted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoFileUploadStartedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                LocalFilePath = localPath
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileDownloadStarted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task UploadVideoFileAsync(
        VideoFileUploadStartedEto @event,
        CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = StatusNames.VideoUploading;
            videoRequest.CurrentStep = EventNames.VideoFileUploadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            // Upload file to CDN using selected provider
            var cdnProviderKey = videoRequest.VideoCdnProviderKey ?? ProviderDefaults.DefaultCdnProvider;

            await using var fileStream = File.OpenRead(@event.LocalFilePath);
            var fileName = Path.GetFileName(@event.LocalFilePath);

            var (storageUrl, cdnUrl) = await storageService.UploadAsync(
                cdnProviderKey,
                fileStream,
                fileName,
                cancellationToken);

            videoRequest.VideoStorageUrl = storageUrl;
            videoRequest.VideoCdnUrl = cdnUrl;
            videoRequest.VideoCdnProviderKey = cdnProviderKey;
            videoRequest.Status = StatusNames.Completed;
            videoRequest.CurrentStep = EventNames.VideoGenerationResultPublished;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoGenerationResultPublishedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                FinalVideoUrl = storageUrl
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileUploadStarted,
                ex,
                cancellationToken);

            return;
        }
    }

    public async Task CompleteAudioUploadManuallyAsync(
        Guid audioRequestId,
        ManualAudioUploadInput input,
        CancellationToken cancellationToken)
    {
        var audio = await GetAudioAsync(audioRequestId, cancellationToken);

        audio.AudioStorageUrl = input.StorageUrl;
        audio.Status = StatusNames.Uploaded;
        audio.CurrentStep = EventNames.AudioFileUploadCompleted;
        audio.LastError = null;
        audio.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(audio, cancellationToken);

        await eventBus.PublishAsync(new AudioFileUploadCompletedEto
        {
            RefContentId = audio.RefContentId,
                RefContentType = audio.RefContentType,
            CorrelationId = input.CorrelationId,
            VideoRequestId = audio.VideoRequestId,
            AudioRequestId = audio.Id,
            StorageUrl = input.StorageUrl,
            IsManual = true
        }, cancellationToken);
    }

    public async Task ScheduleAudioProviderPollingAsync(
        AudioProviderPollingStartedEto @event,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(@event.ProviderKey))
            throw new InvalidOperationException("ProviderKey is required for polling.");

        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        if (audioRequest.Status is StatusNames.AudioProviderCompleted or StatusNames.Uploaded or StatusNames.Failed)
            return;

        audioRequest.AudioProviderTrackingId = @event.ProviderTrackId;
        audioRequest.Status = StatusNames.AudioProviderPolling;
        audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;

        if (audioRequest.NextProviderPollAtUtc is null)
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_videoPollingSettings.ErrorRescheduleDelaySeconds);

        audioRequest.LastError = null;
        audioRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(audioRequest, cancellationToken);
    }

    public async Task ScheduleVideoProviderPollingAsync(
        VideoProviderPollingStartedEto @event,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(@event.ProviderKey))
            throw new InvalidOperationException("ProviderKey is required for polling.");

        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        if (videoRequest.Status is StatusNames.VideoProviderCompleted or StatusNames.Completed or StatusNames.Failed)
            return;

        videoRequest.VideoProviderTrackingId = @event.ProviderTrackId;
        videoRequest.Status = StatusNames.VideoProviderPolling;
        videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;

        if (videoRequest.NextProviderPollAtUtc is null)
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_videoPollingSettings.ErrorRescheduleDelaySeconds);

        videoRequest.LastError = null;
        videoRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(videoRequest, cancellationToken);
    }

    private static List<VideoInputAudioItem> ExtractAudioItems(string videoInputJson)
    {
        using var doc = JsonDocument.Parse(videoInputJson);

        return doc.RootElement
            .GetProperty("audioItems")
            .EnumerateArray()
            .Select(x => new VideoInputAudioItem
            {
                CustomerContentId = x.TryGetProperty("customerContentId", out var customerIdEl)
                    ? customerIdEl.GetGuid()
                    : null,
                SortOrder = x.GetProperty("sortOrder").GetInt32(),
                Text = x.GetProperty("text").GetString() ?? string.Empty
            })
            .ToList();
    }

    private async Task<VideoRequest> GetVideoAsync(Guid id, CancellationToken cancellationToken)
    {
        var video = await context.VideoRequests.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (video == null)
            throw new InvalidOperationException($"VideoRequest not found: {id}");
        return video;
    }

    private async Task<AudioRequest> GetAudioAsync(Guid id, CancellationToken cancellationToken)
    {
        var audio = await context.AudioRequests.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (audio == null)
            throw new InvalidOperationException($"AudioRequest not found: {id}");
        return audio;
    }

    private Task ReplaceVideoAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        return context.VideoRequests.ReplaceOneAsync(x => x.Id == request.Id, request, cancellationToken: cancellationToken);
    }

    private Task ReplaceAudioAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        return context.AudioRequests.ReplaceOneAsync(x => x.Id == request.Id, request, cancellationToken: cancellationToken);
    }

    private async Task HandleAudioExceptionAsync(
        AudioRequest request,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAudioRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailAudioAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleAudioRetryAsync(
        AudioRequest request,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        request.RetryCount++;

        if (request.RetryCount >= 30)
        {
            await FailAudioAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            _retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
            CorrelationId = request.CorrelationId,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }

    private async Task FailAudioAsync(
        AudioRequest request,
        string step,
        Exception ex,
        bool retryable,
        CancellationToken cancellationToken)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
            CorrelationId = request.CorrelationId,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = retryable
        }, cancellationToken);
    }

    private async Task HandleVideoExceptionAsync(
        VideoRequest request,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleVideoRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailVideoAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleVideoRetryAsync(
        VideoRequest request,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        request.RetryCount++;

        if (request.RetryCount >= 30)
        {
            await FailVideoAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            _retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
            CorrelationId = request.CorrelationId,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }

    private async Task FailVideoAsync(
        VideoRequest request,
        string step,
        Exception ex,
        bool retryable,
        CancellationToken cancellationToken)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
            CorrelationId = request.CorrelationId,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = retryable
        }, cancellationToken);
    }

    private async Task<string> DownloadAndUploadToStorageAsync(
        string downloadUrl,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Download file from provider
            var fileContent = await httpClient.GetByteArrayAsync(downloadUrl, cancellationToken);

            // Upload to mock storage
            var storageUrl = $"{storageSettings.BaseUrl}/upload";
            using (var content = new ByteArrayContent(fileContent))
            {
                var response = await httpClient.PostAsync(
                    $"{storageUrl}?fileName={fileName}",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonDoc = JsonDocument.Parse(responseJson);
                var fileId = jsonDoc.RootElement.GetProperty("fileId").GetString()
                    ?? throw new InvalidOperationException("No fileId in storage response");

                // Construct download URL from storage service
                var storageDownloadUrl = $"{storageSettings.BaseUrl}/download/{fileId}";
                return storageDownloadUrl;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download and upload file from {DownloadUrl}", downloadUrl);
            throw;
        }
    }
}

public sealed class VideoInputAudioItem
{
    public Guid? CustomerContentId { get; set; }
    public int SortOrder { get; set; }
    public string Text { get; set; } = default!;
}

public sealed class ManualAudioUploadInput
{
    public string? CorrelationId { get; set; }
    public string ContentProcessType { get; set; } = ContentProcessTypes.CustomerContent;
    public string StorageUrl { get; set; } = default!;
}