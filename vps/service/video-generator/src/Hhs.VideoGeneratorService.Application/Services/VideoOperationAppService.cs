using System.Linq.Expressions;
using System.Text.Json;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Application.Providers.Audio;
using Hhs.VideoGeneratorService.Application.Providers.FileDownloader;
using Hhs.VideoGeneratorService.Application.Providers.Video;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class VideoOperationAppService(
    IServiceProvider provider,
    IVideoRequestRepository videoRequestRepository,
    IAudioRequestRepository audioRequestRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IRemoteFileDownloader remoteFileDownloader,
    ICdnProviderResolver cdnProviderResolver,
    IVideoProviderResolver videoProviderResolver,
    IAudioProviderResolver audioProviderResolver,
    SystemCdnSettings systemCdnSettings,
    RetryDelayCalculator retryDelayCalculator,
    VideoPollingSettings videoPollingSettings,
    VideoRetrySettings serviceRetrySettings) : ApplicationServiceBase(provider)
{
    public async Task CreateVideoRequestAsync(VideoGenerationDataForwardedEto @event, Guid eventId, string correlationId, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<VideoRequest> { Filter = x => x.SourceEventId == eventId };
        var existing = (await videoRequestRepository.GetListAsync(options, cancellationToken)).FirstOrDefault();

        if (existing is not null)
        {
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoRequestCreatedEto { RefContentId = existing.RefContentId, RefContentType = existing.RefContentType, VideoRequestId = existing.Id }
            );
            return;
        }

        var videoProviderKeyResult = await customerVpSettingRepository.GetVideoProviderKeyByScopeKeyAsync(@event.ScopeKey, cancellationToken);
        if (!videoProviderKeyResult.Key)
        {
            throw new InvalidOperationException($"Provider key value is unknown. Scope key: {@event.ScopeKey}");
        }

        var videoProvider = videoProviderResolver.Resolve(videoProviderKeyResult.Value);
        if (videoProvider is null) throw new InvalidOperationException("Video Provider not found.");

        string? audioProviderKey = null;
        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired)
        {
            var audioProviderKeyResult = await customerVpSettingRepository.GetAudioProviderKeyByScopeKeyAsync(@event.ScopeKey, cancellationToken);
            if (!audioProviderKeyResult.Key)
            {
                throw new InvalidOperationException($"Provider key value is unknown. Scope key: {@event.ScopeKey}");
            }

            audioProviderKey = audioProviderKeyResult.Value;
        }

        var videoRequestId = Guid.NewGuid();

        var videoRequest = new VideoRequest(
            videoRequestId,
            @event.ScopeKey,
            @event.RefContentId,
            @event.RefContentType,
            eventId)
        {
            CorrelationId = correlationId,
            Status = StatusNames.Created,
            CurrentStep = EventNames.VideoRequestCreated,
            MediaInputJson = @event.VideoInputJson,
            AudioProviderKey = audioProviderKey,
            VideoProviderKey = videoProviderKeyResult.Value
        };

        await videoRequestRepository.InsertAsync(videoRequest, cancellationToken);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoRequestCreatedEto { RefContentId = @event.RefContentId, RefContentType = @event.RefContentType, VideoRequestId = videoRequestId }
        );
    }

    public async Task StartVideoOperationAsync(VideoRequestCreatedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        videoRequest.Status = StatusNames.Started;
        videoRequest.CurrentStep = EventNames.VideoOperationStarted;

        await ReplaceVideoAsync(videoRequest, cancellationToken);

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoOperationStartedEto { VideoRequestId = videoRequest.Id }
        );
    }

    public async Task HandleVideoOperationStartedAsync(VideoOperationStartedEto @event, Guid eventId, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);
        System.Diagnostics.Debug.WriteLine($"DEBUG: VideoProviderKey={videoRequest.VideoProviderKey}");
        System.Diagnostics.Debug.WriteLine($"DEBUG: MediaInputJson={videoRequest.MediaInputJson}");
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
            var audioOptions = new ListQueryOptions<AudioRequest> { Filter = x => x.VideoRequestId == videoRequest.Id && x.SortOrder == item.SortOrder };
            var existingAudio = (await audioRequestRepository.GetListAsync(audioOptions, cancellationToken)).FirstOrDefault();

            if (existingAudio is not null)
            {
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = existingAudio.Id }
                );

                continue;
            }

            var audioRequestId = Guid.NewGuid();

            var audioRequest = new AudioRequest(
                audioRequestId,
                videoRequest.Id,
                videoRequest.RefContentId,
                videoRequest.RefContentType,
                videoRequest.ScopeKey,
                eventId,
                item.Text,
                videoRequest.AudioProviderKey,
                item.SortOrder) { CorrelationId = videoRequest.CorrelationId, Status = StatusNames.AudioRequestCreated, CurrentStep = EventNames.AudioRequestCreated };

            await audioRequestRepository.InsertAsync(audioRequest, cancellationToken);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = audioRequest.Id }
            );
        }
    }

    public async Task StartAudioProviderRequestAsync(AudioProviderRequestStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);
        var audioProvider = audioProviderResolver.Resolve(audioRequest.AudioProviderKey);

        try
        {
            audioRequest.Status = StatusNames.AudioProviderRequestStarted;
            audioRequest.CurrentStep = EventNames.AudioProviderRequestStarted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var response = await audioProvider.CreateAsync(new AudioCreateRequest { InputText = audioRequest.InputText });

            audioRequest.AudioProviderTrackingId = response.ProviderTrackId;
            audioRequest.AudioProviderUrl = response.ProviderFileUrl;

            if (audioProvider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                audioRequest.Status = StatusNames.AudioProviderCompleted;
                audioRequest.CurrentStep = EventNames.AudioProviderCompleted;

                await ReplaceAudioAsync(audioRequest, cancellationToken);

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

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new AudioProviderPollingStartedEto { AudioRequestId = audioRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioProviderRequestStarted,
                ex,
                cancellationToken
            );

            return;
        }
    }

    public async Task ScheduleAudioProviderPollingAsync(AudioProviderPollingStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        if (audioRequest.Status is StatusNames.AudioProviderCompleted or StatusNames.Uploaded or StatusNames.Failed)
            return;

        audioRequest.Status = StatusNames.AudioProviderPolling;
        audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;

        if (audioRequest.NextProviderPollAtUtc is null)
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);

        audioRequest.LastError = null;

        await ReplaceAudioAsync(audioRequest, cancellationToken);
    }

    public async Task HandleAudioProviderCompletedAsync(AudioProviderCompletedEto @event, CancellationToken cancellationToken = default)
    {
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new AudioFileDownloadStartedEto { AudioRequestId = @event.AudioRequestId }
        );
    }

    public async Task DownloadAudioFileAsync(AudioFileDownloadStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = StatusNames.Downloading;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            if (string.IsNullOrWhiteSpace(audioRequest.AudioProviderUrl))
                throw new InvalidOperationException("Audio provider url is required.");

            (bool success, string audioLocalPath) = await remoteFileDownloader.DownloadAsync(audioRequest.AudioProviderKey, audioRequest.AudioProviderUrl);
            if (!success) throw new InvalidOperationException($"Failed to download audio file: {audioLocalPath}");

            audioRequest.AudioLocalPath = audioLocalPath;

            audioRequest.Status = StatusNames.Downloaded;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadCompleted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

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
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = StatusNames.AudioFileUploading;
            audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

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

            await ReplaceAudioAsync(audioRequest, cancellationToken);

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
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            var providerKeyResult = await customerVpSettingRepository.GetVideoProviderKeyByScopeKeyAsync(videoRequest.ScopeKey, cancellationToken);
            if (!providerKeyResult.Key)
            {
                throw new InvalidOperationException($"Provider key value is unknown. Scope key: {videoRequest.ScopeKey}");
            }

            var videoProvider = videoProviderResolver.Resolve(providerKeyResult.Value);

            var audioOptions = new ListQueryOptions<AudioRequest> { Filter = x => x.VideoRequestId == @event.VideoRequestId };
            var allAudios = await audioRequestRepository.GetListAsync(audioOptions, cancellationToken);

            if (allAudios.Any(x => x.Status == StatusNames.Failed))
            {
                await FailVideoRequestDueToAudioFailureAsync(videoRequest, cancellationToken);
                return;
            }

            if (allAudios.Any(x => x.Status != StatusNames.AudioFileUploadCompleted))
                return;

            var orderedAudios = allAudios.OrderBy(x => x.SortOrder).ToList();

            if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired &&
                orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.AudioCdnUrl)))
            {
                throw new InvalidOperationException("AudioCdnUrl is required for video provider.");
            }

            var lockPredicate = (Expression<Func<VideoRequest, bool>>)(x =>
                x.Id == @event.VideoRequestId &&
                x.Status != StatusNames.VideoProviderRequestStarting &&
                x.Status != StatusNames.VideoProviderRequestStarted &&
                x.Status != StatusNames.VideoProviderPolling &&
                x.Status != StatusNames.VideoProviderCompleted &&
                x.Status != StatusNames.VideoDownloading &&
                x.Status != StatusNames.VideoFileUploading &&
                x.Status != StatusNames.Completed &&
                x.Status != StatusNames.Failed);

            var lockUpdate = Builders<VideoRequest>.Update
                .Set(x => x.Status, StatusNames.VideoProviderRequestStarting)
                .Set(x => x.CurrentStep, EventNames.VideoProviderRequestStarted)
                .Set(x => x.LastError, null);

            var lockResult = await videoRequestRepository.UpdateByExpressionAsync(
                lockPredicate,
                u => lockUpdate,
                cancellationToken: cancellationToken);

            if (lockResult == 0)
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
                ex,
                cancellationToken
            );
        }
    }

    public async Task StartVideoProviderRequestAsync(VideoProviderRequestStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);


        var providerKeyResult = await customerVpSettingRepository.GetVideoProviderKeyByScopeKeyAsync(videoRequest.ScopeKey, cancellationToken);
        if (!providerKeyResult.Key)
        {
            throw new InvalidOperationException($"Provider key value is unknown. Scope key: {videoRequest.ScopeKey}");
        }

        var provider = videoProviderResolver.Resolve(providerKeyResult.Value);

        try
        {
            videoRequest.Status = StatusNames.VideoProviderRequestStarted;
            videoRequest.CurrentStep = EventNames.VideoProviderRequestStarted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var response = await provider.CreateAsync(new VideoCreateRequest { VideoInputJson = videoRequest.MediaInputJson, AudioUrls = @event.AudioUrls });

            videoRequest.VideoProviderTrackingId = response.ProviderTrackId;
            videoRequest.VideoProviderUrl = response.ProviderFileUrl;

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                videoRequest.Status = StatusNames.VideoProviderCompleted;
                videoRequest.CurrentStep = EventNames.VideoProviderCompleted;

                await ReplaceVideoAsync(videoRequest, cancellationToken);

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

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                eventMessage: new VideoProviderPollingStartedEto { VideoRequestId = videoRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoProviderRequestStarted,
                ex,
                cancellationToken
            );

            return;
        }
    }

    public async Task ScheduleVideoProviderPollingAsync(VideoProviderPollingStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        if (videoRequest.Status is StatusNames.VideoProviderCompleted or StatusNames.Completed or StatusNames.Failed)
            return;

        videoRequest.Status = StatusNames.VideoProviderPolling;
        videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;

        if (videoRequest.NextProviderPollAtUtc is null)
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);

        videoRequest.LastError = null;

        await ReplaceVideoAsync(videoRequest, cancellationToken);
    }

    public async Task HandleVideoProviderCompletedAsync(VideoProviderCompletedEto @event, CancellationToken cancellationToken = default)
    {
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            eventMessage: new VideoFileDownloadStartedEto { VideoRequestId = @event.VideoRequestId }
        );
    }

    public async Task DownloadVideoFileAsync(VideoFileDownloadStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = StatusNames.VideoDownloading;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadStarted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            (bool success, string videoLocalPath) = await remoteFileDownloader.DownloadAsync(videoRequest.VideoProviderKey, videoRequest.VideoProviderUrl);
            if (!success) throw new InvalidOperationException($"Failed to download video file: {videoLocalPath}");

            videoRequest.VideoLocalPath = videoLocalPath;

            videoRequest.Status = StatusNames.VideoDownloaded;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadCompleted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

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
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            string cdnProviderKey = systemCdnSettings.Selected;

            videoRequest.Status = StatusNames.VideoFileUploading;
            videoRequest.CurrentStep = EventNames.VideoFileUploadStarted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

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

            await ReplaceVideoAsync(videoRequest, cancellationToken);

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
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = StatusNames.Completed;
            videoRequest.CurrentStep = EventNames.VideoGenerationResultPublished;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

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

    private async Task<VideoRequest> GetVideoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var video = await videoRequestRepository.GetFirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (video == null)
            throw new InvalidOperationException($"VideoRequest not found: {id}");
        return video;
    }

    private async Task<AudioRequest> GetAudioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var audio = await audioRequestRepository.GetFirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (audio == null)
            throw new InvalidOperationException($"AudioRequest not found: {id}");
        return audio;
    }

    private Task ReplaceVideoAsync(VideoRequest request, CancellationToken cancellationToken = default)
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
            .Set(x => x.RetryCount, request.RetryCount)
            .Set(x => x.LastError, request.LastError)
            .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc);

        return videoRequestRepository.UpdateByExpressionAsync(predicate, u => update, cancellationToken: cancellationToken);
    }

    private Task ReplaceAudioAsync(AudioRequest request, CancellationToken cancellationToken = default)
    {
        var predicate = (Expression<Func<AudioRequest, bool>>)(x => x.Id == request.Id);
        var update = Builders<AudioRequest>.Update
            .Set(x => x.Status, request.Status)
            .Set(x => x.CurrentStep, request.CurrentStep)
            .Set(x => x.InputText, request.InputText)
            .Set(x => x.AudioProviderKey, request.AudioProviderKey)
            .Set(x => x.SortOrder, request.SortOrder)
            .Set(x => x.AudioProviderTrackingId, request.AudioProviderTrackingId)
            .Set(x => x.AudioProviderUrl, request.AudioProviderUrl)
            .Set(x => x.NextProviderPollAtUtc, request.NextProviderPollAtUtc)
            .Set(x => x.ProviderPollingCount, request.ProviderPollingCount)
            .Set(x => x.AudioLocalPath, request.AudioLocalPath)
            .Set(x => x.AudioCdnProviderKey, request.AudioCdnProviderKey)
            .Set(x => x.AudioCdnUrl, request.AudioCdnUrl)
            .Set(x => x.AudioStorageUrl, request.AudioStorageUrl)
            .Set(x => x.RetryCount, request.RetryCount)
            .Set(x => x.LastError, request.LastError)
            .Set(x => x.NextRetryAtUtc, request.NextRetryAtUtc);

        return audioRequestRepository.UpdateByExpressionAsync(predicate, u => update, cancellationToken: cancellationToken);
    }

    private async Task HandleAudioExceptionAsync(AudioRequest request, string step, Exception ex, CancellationToken cancellationToken = default)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAudioRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailAudioAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleAudioRetryAsync(AudioRequest request, string step, Exception ex, CancellationToken cancellationToken = default)
    {
        request.RetryCount++;

        if (request.RetryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailAudioAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await ReplaceAudioAsync(request, cancellationToken);

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

    private async Task FailAudioAsync(AudioRequest request, string step, Exception ex, bool retryable, CancellationToken cancellationToken = default)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        await ReplaceAudioAsync(request, cancellationToken);

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

        var parentVideoRequest = await GetVideoAsync(request.VideoRequestId, cancellationToken);
        if (parentVideoRequest != null && parentVideoRequest.Status != StatusNames.Failed && parentVideoRequest.Status != StatusNames.Completed)
        {
            await FailVideoRequestDueToAudioFailureAsync(parentVideoRequest, cancellationToken);
        }
    }

    private async Task HandleVideoExceptionAsync(VideoRequest request, string step, Exception ex, CancellationToken cancellationToken = default)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleVideoRetryAsync(request, step, ex, cancellationToken);
            return;
        }

        await FailVideoAsync(request, step, ex, false, cancellationToken);
    }

    private async Task ScheduleVideoRetryAsync(VideoRequest request, string step, Exception ex, CancellationToken cancellationToken = default)
    {
        request.RetryCount++;

        if (request.RetryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailVideoAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = StatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await ReplaceVideoAsync(request, cancellationToken);

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

    private async Task FailVideoAsync(VideoRequest request, string step, Exception ex, bool retryable, CancellationToken cancellationToken = default)
    {
        request.Status = StatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        await ReplaceVideoAsync(request, cancellationToken);

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

    private async Task FailVideoRequestDueToAudioFailureAsync(VideoRequest videoRequest, CancellationToken cancellationToken = default)
    {
        videoRequest.Status = StatusNames.Failed;
        videoRequest.CurrentStep = EventNames.AudioFileUploadCompleted;
        videoRequest.LastError = "One or more audio requests failed.";
        videoRequest.NextRetryAtUtc = null;

        await ReplaceVideoAsync(videoRequest, cancellationToken);

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
    }

    #endregion
}

public sealed class VideoInputAudioItem
{
    public Guid? CustomerContentId { get; set; }
    public int SortOrder { get; set; }
    public string Text { get; set; } = default!;
}