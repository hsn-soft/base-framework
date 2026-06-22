using System.Text.Json;
using Hhs.Shared.Configuration;
using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Retry;
using Hhs.VideoGeneratorService.Configuration;
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
    ICdnProviderResolver cdnProviderResolver,
    IEventBus eventBus,
    IVideoProviderResolver videoProviderResolver,
    IAudioProviderResolver audioProviderResolver,
    SystemCdnSettings systemCdnSettings,
    RetryDelayCalculator retryDelayCalculator,
    VideoPollingSettings videoPollingSettings)
{
    public async Task CreateVideoRequestAsync(VideoGenerationApprovedEto @event, CancellationToken cancellationToken)
    {
        var existing = await context.VideoRequests
            .Find(x => x.SourceEventId == @event.EventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            var providerForExisting = videoProviderResolver.Resolve(SubscriptionScopeRegistry.GetVideoProviderKey(existing.ScopeKey));
            bool existingExternalAudioRequired =
                providerForExisting.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired ||
                providerForExisting.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired;

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

        string scopeKey = @event.ScopeKey ?? "unknown";
        string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(scopeKey);
        string? audioProviderKey = SubscriptionScopeRegistry.GetAudioProviderKey(scopeKey);
        var videoProvider = videoProviderResolver.Resolve(videoProviderKey);

        var videoRequestId = Guid.NewGuid();

        var videoRequest = new VideoRequest
        {
            Id = videoRequestId,
            SourceEventId = @event.EventId,
            CorrelationId = @event.CorrelationId,
            ScopeKey = scopeKey,
            RefContentId = @event.RefContentId,
            RefContentType = @event.RefContentType,
            Status = StatusNames.Created,
            CurrentStep = EventNames.VideoRequestCreated,
            MediaInputJson = @event.VideoInputJson,
            VideoProviderKey = videoProviderKey,
            AudioProviderKey = audioProviderKey,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await context.VideoRequests.InsertOneAsync(videoRequest, cancellationToken: cancellationToken);

        bool externalAudioRequired =
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

    public async Task StartVideoOperationAsync(VideoRequestCreatedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);
        string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(videoRequest.ScopeKey);
        var videoProvider = videoProviderResolver.Resolve(videoProviderKey);
        bool externalAudioRequired =
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

    public async Task HandleVideoOperationStartedAsync(VideoOperationStartedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);
        string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(videoRequest.ScopeKey);
        var videoProvider = videoProviderResolver.Resolve(videoProviderKey);

        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.ProviderCreatesAudio ||
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.NoAudio)
        {
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
                SourceEventId = @event.EventId,
                VideoRequestId = videoRequest.Id,
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                ScopeKey = videoRequest.ScopeKey,
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

    public async Task StartAudioProviderRequestAsync(AudioProviderRequestStartedEto @event, CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);
        string? audioProviderKey = SubscriptionScopeRegistry.GetAudioProviderKey(audioRequest.ScopeKey);
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

                audioRequest.Status = StatusNames.AudioProviderCompleted;
                audioRequest.CurrentStep = EventNames.AudioProviderCompleted;
                audioRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceAudioAsync(audioRequest, cancellationToken);

                // Publish provider completed event - handler will trigger download cascade
                await eventBus.PublishAsync(new AudioProviderCompletedEto
                {
                    RefContentId = audioRequest.RefContentId,
                    RefContentType = @event.RefContentType,
                    CorrelationId = @event.CorrelationId,
                    VideoRequestId = audioRequest.VideoRequestId,
                    AudioRequestId = audioRequest.Id,
                    ProviderFileUrl = response.ProviderFileUrl
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Audio provider track id is required.");

            audioRequest.AudioProviderTrackingId = response.ProviderTrackId;
            audioRequest.Status = StatusNames.AudioProviderPolling;
            audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
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

    public async Task ScheduleAudioProviderPollingAsync(AudioProviderPollingStartedEto @event, CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        if (audioRequest.Status is StatusNames.AudioProviderCompleted or StatusNames.Uploaded or StatusNames.Failed)
            return;

        audioRequest.AudioProviderTrackingId = @event.ProviderTrackId;
        audioRequest.Status = StatusNames.AudioProviderPolling;
        audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;

        if (audioRequest.NextProviderPollAtUtc is null)
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);

        audioRequest.LastError = null;
        audioRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(audioRequest, cancellationToken);
    }

    public async Task HandleAudioProviderCompletedAsync(AudioProviderCompletedEto @event, CancellationToken cancellationToken)
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

    public async Task DownloadAudioFileAsync(AudioFileDownloadStartedEto @event, CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = StatusNames.Downloading;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            string localPath = await fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp3", $"audio_{audioRequest.Id:N}", cancellationToken);

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

    public async Task HandleAudioDownloadCompletedAsync(AudioFileDownloadCompletedEto @event, CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            string cdnProviderKey = systemCdnSettings.Selected;

            audioRequest.Status = StatusNames.AudioFileUploading;
            audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            // Upload file to CDN using resolved CDN provider
            await using var fileStream = File.OpenRead(@event.LocalFilePath);
            string fileName = Path.GetFileName(@event.LocalFilePath);

            var cdnProvider = cdnProviderResolver.Resolve(cdnProviderKey);
            (string storageUrl, string cdnUrl) = await cdnProvider.UploadAsync(
                fileStream,
                fileName,
                cancellationToken);

            audioRequest.AudioStorageUrl = storageUrl;
            audioRequest.AudioCdnUrl = cdnUrl;
            audioRequest.AudioCdnProviderKey = cdnProviderKey;
            audioRequest.Status = StatusNames.AudioFileUploadCompleted;
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
                StorageUrl = storageUrl,
                CdnFileUrl = cdnUrl,
                CdnProviderKey = cdnProviderKey
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

    public async Task HandleAudioUploadCompletedAsync(AudioFileUploadCompletedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(videoRequest.ScopeKey);
            var videoProvider = videoProviderResolver.Resolve(videoProviderKey);

            var allAudios = await context.AudioRequests
                .Find(x => x.VideoRequestId == @event.VideoRequestId)
                .ToListAsync(cancellationToken);

            if (allAudios.Any(x => x.Status != StatusNames.AudioFileUploadCompleted))
                return;

            var orderedAudios = allAudios.OrderBy(x => x.SortOrder).ToList();

            if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired &&
                orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.AudioCdnUrl)))
            {
                throw new InvalidOperationException("AudioCdnUrl is required for video provider.");
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

    public async Task StartVideoProviderRequestAsync(VideoProviderRequestStartedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);
        string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(videoRequest.ScopeKey);
        var provider = videoProviderResolver.Resolve(videoProviderKey);

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

                videoRequest.Status = StatusNames.VideoProviderCompleted;
                videoRequest.CurrentStep = EventNames.VideoProviderCompleted;
                videoRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(videoRequest, cancellationToken);

                // Publish provider completed event - handler will trigger download cascade
                await eventBus.PublishAsync(new VideoProviderCompletedEto
                {
                    RefContentId = videoRequest.RefContentId,
                    RefContentType = videoRequest.RefContentType,
                    CorrelationId = @event.CorrelationId,
                    VideoRequestId = videoRequest.Id,
                    ProviderFileUrl = response.ProviderFileUrl
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Video provider track id is required.");

            videoRequest.VideoProviderTrackingId = response.ProviderTrackId;
            videoRequest.Status = StatusNames.VideoProviderPolling;
            videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
            videoRequest.ProviderPollingCount = 0;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoProviderPollingStartedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
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

    public async Task ScheduleVideoProviderPollingAsync(VideoProviderPollingStartedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        if (videoRequest.Status is StatusNames.VideoProviderCompleted or StatusNames.Completed or StatusNames.Failed)
            return;

        videoRequest.VideoProviderTrackingId = @event.ProviderTrackId;
        videoRequest.Status = StatusNames.VideoProviderPolling;
        videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;

        if (videoRequest.NextProviderPollAtUtc is null)
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);

        videoRequest.LastError = null;
        videoRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(videoRequest, cancellationToken);
    }

    public async Task HandleVideoProviderCompletedAsync(VideoProviderCompletedEto @event, CancellationToken cancellationToken)
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

    public async Task DownloadVideoFileAsync(VideoFileDownloadStartedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = StatusNames.VideoDownloading;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            string localPath = await fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp4", $"video_{videoRequest.Id:N}", cancellationToken);

            videoRequest.VideoLocalPath = localPath;
            videoRequest.Status = StatusNames.VideoDownloaded;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadCompleted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoFileDownloadCompletedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                LocalFilePath = localPath,
                ProviderFileUrl = @event.ProviderFileUrl
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

    public async Task HandleVideoDownloadCompletedAsync(VideoFileDownloadCompletedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            string cdnProviderKey = systemCdnSettings.Selected;

            videoRequest.Status = StatusNames.VideoFileUploading;
            videoRequest.CurrentStep = EventNames.VideoFileUploadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            // Upload file to CDN using resolved CDN provider
            await using var fileStream = File.OpenRead(@event.LocalFilePath);
            string fileName = Path.GetFileName(@event.LocalFilePath);

            var cdnProvider = cdnProviderResolver.Resolve(cdnProviderKey);
            (string storageUrl, string cdnUrl) = await cdnProvider.UploadAsync(
                fileStream,
                fileName,
                cancellationToken);

            videoRequest.VideoStorageUrl = storageUrl;
            videoRequest.VideoCdnUrl = cdnUrl;
            videoRequest.VideoCdnProviderKey = cdnProviderKey;
            videoRequest.Status = StatusNames.VideoFileUploadCompleted;
            videoRequest.CurrentStep = EventNames.VideoFileUploadCompleted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoFileUploadCompletedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                StorageUrl = storageUrl,
                CdnFileUrl = cdnUrl,
                CdnProviderKey = cdnProviderKey
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

    public async Task HandleVideoUploadCompletedAsync(VideoFileUploadCompletedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
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
                FinalVideoUrl = @event.CdnFileUrl
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileUploadCompleted,
                ex,
                cancellationToken);

            return;
        }
    }

    #region Private methods

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

    private async Task HandleAudioExceptionAsync(AudioRequest request, string step, Exception ex, CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAudioRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailAudioAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleAudioRetryAsync(AudioRequest request, string step, Exception ex, CancellationToken cancellationToken)
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
            retryDelayCalculator.Calculate(request.RetryCount));

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

    private async Task FailAudioAsync(AudioRequest request, string step, Exception ex, bool retryable, CancellationToken cancellationToken)
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

    private async Task HandleVideoExceptionAsync(VideoRequest request, string step, Exception ex, CancellationToken cancellationToken)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleVideoRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailVideoAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleVideoRetryAsync(VideoRequest request, string step, Exception ex, CancellationToken cancellationToken)
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
            retryDelayCalculator.Calculate(request.RetryCount));

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

    private async Task FailVideoAsync(VideoRequest request, string step, Exception ex, bool retryable, CancellationToken cancellationToken)
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

    #endregion
}

public sealed class VideoInputAudioItem
{
    public Guid? CustomerContentId { get; set; }
    public int SortOrder { get; set; }
    public string Text { get; set; } = default!;
}