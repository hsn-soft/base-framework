using System.Text.Json;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Application.Providers.Audio;
using Hhs.VideoGeneratorService.Application.Providers.FileDownloader;
using Hhs.VideoGeneratorService.Application.Providers.Video;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.EventBus;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoOperationAppService(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext context,
    IRemoteFileDownloader remoteFileDownloader,
    ICdnProviderResolver cdnProviderResolver,
    IVideoProviderResolver videoProviderResolver,
    IAudioProviderResolver audioProviderResolver,
    SystemCdnSettings systemCdnSettings,
    RetryDelayCalculator retryDelayCalculator,
    VideoPollingSettings videoPollingSettings) : ApplicationServiceBase(provider)
{
    public async Task CreateVideoRequestAsync(VideoGenerationApprovedEto @event, Guid eventId, string correlationId, CancellationToken cancellationToken = default)
    {
        var existing = await context.VideoRequests
            .Find(x => x.SourceEventId == eventId)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoRequestCreatedEto { RefContentId = existing.RefContentId, RefContentType = existing.RefContentType, VideoRequestId = existing.Id }
            );
            return;
        }

        string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(@event.ScopeKey);
        if (videoProviderKey is null) throw new InvalidOperationException("VideoProviderKey is required.");

        var videoProvider = videoProviderResolver.Resolve(videoProviderKey);
        if (videoProvider is null) throw new InvalidOperationException("Video Provider not found.");

        string? audioProviderKey = null;
        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired)
        {
            audioProviderKey = SubscriptionScopeRegistry.GetAudioProviderKey(@event.ScopeKey);
            if (audioProviderKey is null) throw new InvalidOperationException("AudioProviderKey is required.");
        }

        var videoRequestId = Guid.NewGuid();

        var videoRequest = new VideoRequest
        {
            Id = videoRequestId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId,
            SourceEventId = eventId,
            RefContentId = @event.RefContentId,
            RefContentType = @event.RefContentType,
            ScopeKey = @event.ScopeKey,
            Status = StatusNames.Created,
            CurrentStep = EventNames.VideoRequestCreated,
            MediaInputJson = @event.VideoInputJson,
            AudioProviderKey = audioProviderKey,
            VideoProviderKey = videoProviderKey,
            VideoProviderTrackingId = null,
            VideoProviderUrl = null,
            VideoLocalPath = null,
            VideoCdnProviderKey = null,
            VideoStorageUrl = null,
            VideoCdnUrl = null,
            NextProviderPollAtUtc = null,
            ProviderPollingCount = 0,
            RetryCount = 0,
            NextRetryAtUtc = null,
            LastError = null
        };

        await context.VideoRequests.InsertOneAsync(videoRequest);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoRequestCreatedEto { RefContentId = @event.RefContentId, RefContentType = @event.RefContentType, VideoRequestId = videoRequestId }
        );
    }

    public async Task StartVideoOperationAsync(VideoRequestCreatedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);

        videoRequest.Status = StatusNames.Started;
        videoRequest.CurrentStep = EventNames.VideoOperationStarted;
        videoRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(videoRequest);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoOperationStartedEto { VideoRequestId = videoRequest.Id }
        );
    }

    public async Task HandleVideoOperationStartedAsync(VideoOperationStartedEto @event, Guid eventId, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);
        var videoProvider = videoProviderResolver.Resolve(videoRequest.VideoProviderKey);

        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.ProviderCreatesAudio)
        {
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoProviderRequestStartedEto { VideoRequestId = videoRequest.Id, AudioUrls = [] }
            );

            return;
        }

        if (videoRequest.AudioProviderKey is null) throw new InvalidOperationException("AudioProviderKey is required.");

        var audioProvider = audioProviderResolver.Resolve(videoRequest.AudioProviderKey);
        if (audioProvider is null) throw new InvalidOperationException("Audio Provider not found.");

        var audioItems = ExtractAudioItems(videoRequest.MediaInputJson);

        foreach (var item in audioItems)
        {
            var existingAudio = await context.AudioRequests
                .Find(x => x.VideoRequestId == videoRequest.Id && x.SortOrder == item.SortOrder)
                .FirstOrDefaultAsync();

            if (existingAudio is not null)
            {
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = existingAudio.Id }
                );

                continue;
            }

            var audioRequestId = Guid.NewGuid();

            var audioRequest = new AudioRequest
            {
                Id = audioRequestId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CorrelationId = videoRequest.CorrelationId,
                SourceEventId = eventId,
                VideoRequestId = videoRequest.Id,
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                ScopeKey = videoRequest.ScopeKey,
                Status = StatusNames.AudioRequestCreated,
                CurrentStep = EventNames.AudioRequestCreated,
                SortOrder = item.SortOrder,
                InputText = item.Text,
                AudioProviderKey = videoRequest.AudioProviderKey,
                AudioProviderTrackingId = null,
                AudioProviderUrl = null,
                AudioLocalPath = null,
                AudioStorageUrl = null,
                AudioCdnUrl = null,
                AudioCdnProviderKey = null,
                NextProviderPollAtUtc = null,
                ProviderPollingCount = 0,
                RetryCount = 0,
                NextRetryAtUtc = null,
                LastError = null
            };

            await context.AudioRequests.InsertOneAsync(audioRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = audioRequest.Id }
            );
        }
    }

    public async Task StartAudioProviderRequestAsync(AudioProviderRequestStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId);
        var audioProvider = audioProviderResolver.Resolve(audioRequest.AudioProviderKey);

        try
        {
            audioRequest.Status = StatusNames.AudioProviderRequestStarted;
            audioRequest.CurrentStep = EventNames.AudioProviderRequestStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest);

            var response = await audioProvider.CreateAsync(new AudioCreateRequest { InputText = audioRequest.InputText });

            audioRequest.AudioProviderTrackingId = response.ProviderTrackId;
            audioRequest.AudioProviderUrl = response.ProviderFileUrl;

            if (audioProvider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                audioRequest.Status = StatusNames.AudioProviderCompleted;
                audioRequest.CurrentStep = EventNames.AudioProviderCompleted;
                audioRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceAudioAsync(audioRequest);

                // Publish provider completed event - handler will trigger download cascade
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new AudioProviderCompletedEto { AudioRequestId = audioRequest.Id }
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Audio provider track id is required.");

            audioRequest.Status = StatusNames.AudioProviderPolling;
            audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
            audioRequest.ProviderPollingCount = 0;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioProviderPollingStartedEto { AudioRequestId = audioRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioProviderRequestStarted,
                ex
            );

            return;
        }
    }

    public async Task ScheduleAudioProviderPollingAsync(AudioProviderPollingStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId);

        if (audioRequest.Status is StatusNames.AudioProviderCompleted or StatusNames.Uploaded or StatusNames.Failed)
            return;

        audioRequest.Status = StatusNames.AudioProviderPolling;
        audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;

        if (audioRequest.NextProviderPollAtUtc is null)
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);

        audioRequest.LastError = null;
        audioRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(audioRequest);
    }

    public async Task HandleAudioProviderCompletedAsync(AudioProviderCompletedEto @event, CancellationToken cancellationToken = default)
    {
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AudioFileDownloadStartedEto { AudioRequestId = @event.AudioRequestId }
        );
    }

    public async Task DownloadAudioFileAsync(AudioFileDownloadStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId);

        try
        {
            audioRequest.Status = StatusNames.Downloading;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest);

            if (string.IsNullOrWhiteSpace(audioRequest.AudioProviderUrl))
                throw new InvalidOperationException("Audio provider url is required.");

            (bool success, string audioLocalPath) = await remoteFileDownloader.DownloadAsync(audioRequest.AudioProviderKey, audioRequest.AudioProviderUrl);
            if (!success) throw new InvalidOperationException($"Failed to download audio file: {audioLocalPath}");

            audioRequest.AudioLocalPath = audioLocalPath;

            audioRequest.Status = StatusNames.Downloaded;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioFileDownloadCompletedEto { AudioRequestId = audioRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioFileDownloadStarted,
                ex
            );

            return;
        }
    }

    public async Task HandleAudioDownloadCompletedAsync(AudioFileDownloadCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId);

        try
        {
            audioRequest.Status = StatusNames.AudioFileUploading;
            audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest);

            if (string.IsNullOrWhiteSpace(audioRequest.AudioLocalPath))
                throw new InvalidOperationException("Audio local path is required.");

            if (!File.Exists(audioRequest.AudioLocalPath))
                throw new InvalidOperationException("Audio local file is not exist: " + audioRequest.AudioLocalPath);

            // Upload file to CDN using resolved CDN provider
            await using var fileStream = File.OpenRead(audioRequest.AudioLocalPath);
            string fileName = Path.GetFileName(audioRequest.AudioLocalPath);

            if (string.IsNullOrWhiteSpace(systemCdnSettings.Selected))
                throw new InvalidOperationException("Cdn Provider Key is required.");

            var cdnProvider = cdnProviderResolver.Resolve(systemCdnSettings.Selected);
            if (cdnProvider is null) throw new InvalidOperationException("Cdn Provider not found.");

            (string storageUrl, string cdnUrl) = await cdnProvider.UploadAsync(
                fileStream,
                fileName
            );

            audioRequest.AudioCdnProviderKey = systemCdnSettings.Selected;
            audioRequest.AudioCdnUrl = cdnUrl;
            audioRequest.AudioStorageUrl = storageUrl;

            audioRequest.Status = StatusNames.AudioFileUploadCompleted;
            audioRequest.CurrentStep = EventNames.AudioFileUploadCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioFileUploadCompletedEto { VideoRequestId = audioRequest.VideoRequestId }
            );
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioFileUploadStarted,
                ex
            );

            return;
        }
    }

    public async Task HandleAudioUploadCompletedAsync(AudioFileUploadCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);

        try
        {
            string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(videoRequest.ScopeKey);
            var videoProvider = videoProviderResolver.Resolve(videoProviderKey);

            var allAudios = await context.AudioRequests
                .Find(x => x.VideoRequestId == @event.VideoRequestId)
                .ToListAsync();

            if (allAudios.Any(x => x.Status != StatusNames.AudioFileUploadCompleted))
                return;

            var orderedAudios = allAudios.OrderBy(x => x.SortOrder).ToList();

            if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired &&
                orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.AudioCdnUrl)))
            {
                throw new InvalidOperationException("AudioCdnUrl is required for video provider.");
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
                    .Set(x => x.UpdatedAtUtc, DateTime.UtcNow)
            );

            if (lockResult.ModifiedCount == 0)
                return;

            videoRequest.Status = StatusNames.VideoProviderRequestStarting;

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoProviderRequestStartedEto
                {
                    VideoRequestId = videoRequest.Id,
                    AudioUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                        ? orderedAudios.Select(x => x.AudioStorageUrl!).ToList()
                        : []
                }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoProviderRequestStarted,
                ex
            );
        }
    }

    public async Task StartVideoProviderRequestAsync(VideoProviderRequestStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);
        string? videoProviderKey = SubscriptionScopeRegistry.GetVideoProviderKey(videoRequest.ScopeKey);
        var provider = videoProviderResolver.Resolve(videoProviderKey);

        try
        {
            videoRequest.Status = StatusNames.VideoProviderRequestStarted;
            videoRequest.CurrentStep = EventNames.VideoProviderRequestStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest);

            var response = await provider.CreateAsync(new VideoCreateRequest { VideoInputJson = videoRequest.MediaInputJson, AudioUrls = @event.AudioUrls });

            videoRequest.VideoProviderTrackingId = response.ProviderTrackId;
            videoRequest.VideoProviderUrl = response.ProviderFileUrl;

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                videoRequest.Status = StatusNames.VideoProviderCompleted;
                videoRequest.CurrentStep = EventNames.VideoProviderCompleted;
                videoRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(videoRequest);

                // Publish provider completed event - handler will trigger download cascade
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new VideoProviderCompletedEto { VideoRequestId = videoRequest.Id }
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Video provider track id is required.");

            videoRequest.Status = StatusNames.VideoProviderPolling;
            videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
            videoRequest.ProviderPollingCount = 0;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoProviderPollingStartedEto { VideoRequestId = videoRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoProviderRequestStarted,
                ex
            );

            return;
        }
    }

    public async Task ScheduleVideoProviderPollingAsync(VideoProviderPollingStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);

        if (videoRequest.Status is StatusNames.VideoProviderCompleted or StatusNames.Completed or StatusNames.Failed)
            return;

        videoRequest.Status = StatusNames.VideoProviderPolling;
        videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;

        if (videoRequest.NextProviderPollAtUtc is null)
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);

        videoRequest.LastError = null;
        videoRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(videoRequest);
    }

    public async Task HandleVideoProviderCompletedAsync(VideoProviderCompletedEto @event, CancellationToken cancellationToken = default)
    {
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoFileDownloadStartedEto { VideoRequestId = @event.VideoRequestId }
        );
    }

    public async Task DownloadVideoFileAsync(VideoFileDownloadStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);

        try
        {
            videoRequest.Status = StatusNames.VideoDownloading;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest);

            (bool success, string videoLocalPath) = await remoteFileDownloader.DownloadAsync(videoRequest.VideoProviderKey, videoRequest.VideoProviderUrl);
            if (!success) throw new InvalidOperationException($"Failed to download video file: {videoLocalPath}");

            videoRequest.VideoLocalPath = videoLocalPath;

            videoRequest.Status = StatusNames.VideoDownloaded;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadCompleted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoFileDownloadCompletedEto { VideoRequestId = videoRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileDownloadStarted,
                ex
            );

            return;
        }
    }

    public async Task HandleVideoDownloadCompletedAsync(VideoFileDownloadCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);

        try
        {
            string cdnProviderKey = systemCdnSettings.Selected;

            videoRequest.Status = StatusNames.VideoFileUploading;
            videoRequest.CurrentStep = EventNames.VideoFileUploadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest);

            // Upload file to CDN using resolved CDN provider
            await using var fileStream = File.OpenRead(videoRequest.VideoLocalPath);
            string fileName = Path.GetFileName(videoRequest.VideoLocalPath);

            var cdnProvider = cdnProviderResolver.Resolve(cdnProviderKey);
            (string storageUrl, string cdnUrl) = await cdnProvider.UploadAsync(
                fileStream,
                fileName
            );

            videoRequest.VideoCdnProviderKey = cdnProviderKey;
            videoRequest.VideoCdnUrl = cdnUrl;
            videoRequest.VideoStorageUrl = storageUrl;

            videoRequest.Status = StatusNames.VideoFileUploadCompleted;
            videoRequest.CurrentStep = EventNames.VideoFileUploadCompleted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoFileUploadCompletedEto { VideoRequestId = videoRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileUploadStarted,
                ex
            );

            return;
        }
    }

    public async Task HandleVideoUploadCompletedAsync(VideoFileUploadCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId);

        try
        {
            videoRequest.Status = StatusNames.Completed;
            videoRequest.CurrentStep = EventNames.VideoGenerationResultPublished;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoGenerationResultPublishedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, FinalVideoUrl = videoRequest.VideoCdnUrl }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileUploadCompleted,
                ex
            );

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

    private async Task<VideoRequest> GetVideoAsync(Guid id)
    {
        var video = await context.VideoRequests.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (video == null)
            throw new InvalidOperationException($"VideoRequest not found: {id}");
        return video;
    }

    private async Task<AudioRequest> GetAudioAsync(Guid id)
    {
        var audio = await context.AudioRequests.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (audio == null)
            throw new InvalidOperationException($"AudioRequest not found: {id}");
        return audio;
    }

    private Task ReplaceVideoAsync(VideoRequest request)
    {
        return context.VideoRequests.ReplaceOneAsync(x => x.Id == request.Id, request);
    }

    private Task ReplaceAudioAsync(AudioRequest request)
    {
        return context.AudioRequests.ReplaceOneAsync(x => x.Id == request.Id, request);
    }

    private async Task HandleAudioExceptionAsync(AudioRequest request, string step, Exception ex)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAudioRetryAsync(request, step, ex);
            return;
        }

        await FailAudioAsync(request, step, ex, false);
    }

    private async Task ScheduleAudioRetryAsync(AudioRequest request, string step, Exception ex)
    {
        request.RetryCount++;

        if (request.RetryCount >= 30)
        {
            await FailAudioAsync(request, step, ex, false);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(request);

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = true
            }
        );
    }

    private async Task FailAudioAsync(AudioRequest request, string step, Exception ex, bool retryable)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(request);

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = retryable
            }
        );
    }

    private async Task HandleVideoExceptionAsync(VideoRequest request, string step, Exception ex)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleVideoRetryAsync(request, step, ex);
            return;
        }

        await FailVideoAsync(request, step, ex, false);
    }

    private async Task ScheduleVideoRetryAsync(VideoRequest request, string step, Exception ex)
    {
        request.RetryCount++;

        if (request.RetryCount >= 30)
        {
            await FailVideoAsync(request, step, ex, false);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(request);

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = true
            }
        );
    }

    private async Task FailVideoAsync(VideoRequest request, string step, Exception ex, bool retryable)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(request);

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Step = step,
                ErrorMessage = ex.Message,
                Retryable = retryable
            }
        );
    }

    #endregion
}

public sealed class VideoInputAudioItem
{
    public Guid? CustomerContentId { get; set; }
    public int SortOrder { get; set; }
    public string Text { get; set; } = default!;
}