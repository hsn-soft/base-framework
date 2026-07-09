using System.Linq.Expressions;
using System.Text.Json;
using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Consts;
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
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using Microsoft.Extensions.DependencyInjection;
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
    VideoRetrySettings serviceRetrySettings,
    VideoOperationRetryWorkerService videoOperationRetryWorkerService) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task CreateVideoRequestAsync(VideoGenerationDataForwardedEto @event, Guid eventId, string correlationId, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<VideoRequest> { Filter = x => x.SourceEventId == eventId };
        var existing = (await videoRequestRepository.GetListAsync(options, cancellationToken)).FirstOrDefault();

        if (existing is not null)
        {
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: existing.CorrelationId,
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

        string audioProviderKey = null;
        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired)
        {
            var audioProviderKeyResult = await customerVpSettingRepository.GetAudioProviderKeyByScopeKeyAsync(@event.ScopeKey, cancellationToken);
            if (!audioProviderKeyResult.Key)
            {
                throw new InvalidOperationException($"Provider key value is unknown. Scope key: {@event.ScopeKey}");
            }

            audioProviderKey = audioProviderKeyResult.Value;
        }

        var videoRequestId = Guid.CreateVersion7();

        var videoRequest = new VideoRequest(
            videoRequestId,
            @event.ScopeKey,
            @event.RefContentId,
            @event.RefContentType,
            eventId)
        {
            CorrelationId = correlationId,
            Status = VideoStatusNames.Created,
            CurrentStep = EventNames.VideoRequestCreated,
            MediaInputJson = @event.VideoInputJson,
            AudioProviderKey = audioProviderKey,
            VideoProviderKey = videoProviderKeyResult.Value
        };

        await videoRequestRepository.InsertAsync(videoRequest, cancellationToken);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.VideoRequestCreated,
            reference: new
            {
                @event.ScopeKey,
                Type = nameof(VideoRequest),
                Key = videoRequestId,
                RefType = @event.RefContentType.ToString(),
                RefKey = @event.RefContentId
            },
            facility: Facilities.VideoRequestCreated,
            correlationId: correlationId,
            exception: null
        ));

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            correlationId: correlationId,
            eventMessage: new VideoRequestCreatedEto { RefContentId = @event.RefContentId, RefContentType = @event.RefContentType, VideoRequestId = videoRequestId }
        );
    }

    public async Task StartVideoOperationAsync(VideoRequestCreatedEto @event, Guid eventId, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        videoRequest.Status = VideoStatusNames.Started;
        videoRequest.CurrentStep = EventNames.VideoOperationStarted;

        long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
            validPriorStatuses: [VideoStatusNames.Created, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
        if (claimed == 0) return;

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.VideoOperationStarted,
            reference: new
            {
                videoRequest.ScopeKey,
                Type = nameof(VideoRequest),
                Key = videoRequest.Id,
                RefType = videoRequest.RefContentType.ToString(),
                RefKey = videoRequest.RefContentId
            },
            facility: Facilities.VideoOperationStarted,
            correlationId: videoRequest.CorrelationId,
            exception: null
        ));

        try
        {
            var videoProvider = videoProviderResolver.Resolve(videoRequest.VideoProviderKey);

            if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.ProviderCreatesAudio)
            {
                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.VideoAudioInternal,
                    reference: new
                    {
                        videoRequest.ScopeKey,
                        Type = nameof(VideoRequest),
                        Key = videoRequest.Id,
                        RefType = videoRequest.RefContentType.ToString(),
                        RefKey = videoRequest.RefContentId
                    },
                    facility: Facilities.VideoAudioInternal,
                    correlationId: videoRequest.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: videoRequest.CorrelationId,
                    eventMessage: new AudioOperationStartedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, AudioMode = EventNames.VideoAudioInternal }
                );

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: videoRequest.CorrelationId,
                    eventMessage: new VideoProviderRequestStartedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, AudioCdnUrls = [] }
                );

                return;
            }

            if (videoRequest.AudioProviderKey is null) throw new InvalidOperationException("AudioProviderKey is required.");

            var audioProvider = audioProviderResolver.Resolve(videoRequest.AudioProviderKey);
            if (audioProvider is null) throw new InvalidOperationException("Audio Provider not found.");

            var audioItems = ExtractAudioItems(videoRequest.MediaInputJson);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.VideoAudioExternal,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = nameof(VideoRequest),
                    Key = videoRequest.Id,
                    RefType = videoRequest.RefContentType.ToString(),
                    RefKey = videoRequest.RefContentId,
                    AudioCount = audioItems.Count
                },
                facility: Facilities.VideoAudioExternal,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: videoRequest.CorrelationId,
                eventMessage: new AudioOperationStartedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, AudioMode = EventNames.VideoAudioExternal }
            );

            foreach (var item in audioItems)
            {
                var audioOptions = new ListQueryOptions<AudioRequest> { Filter = x => x.VideoRequestId == videoRequest.Id && x.SortOrder == item.SortOrder };
                var existingAudio = (await audioRequestRepository.GetListAsync(audioOptions, cancellationToken)).FirstOrDefault();

                if (existingAudio is not null)
                {
                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: EventNames.AudioProviderRequestStarted,
                        reference: new
                        {
                            videoRequest.ScopeKey,
                            Type = nameof(AudioRequest),
                            Key = existingAudio.Id,
                            RefType = videoRequest.RefContentType.ToString(),
                            RefKey = videoRequest.RefContentId,
                            VideoRequestId = videoRequest.Id,
                            item.SortOrder
                        },
                        facility: Facilities.AudioProviderRequestStarted,
                        correlationId: videoRequest.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: videoRequest.CorrelationId,
                        eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = existingAudio.Id }
                    );

                    continue;
                }

                var audioRequestId = Guid.CreateVersion7();

                var audioRequest = new AudioRequest(
                    audioRequestId,
                    videoRequest.Id,
                    videoRequest.RefContentId,
                    videoRequest.RefContentType,
                    videoRequest.ScopeKey,
                    eventId,
                    item.Text,
                    videoRequest.AudioProviderKey,
                    item.SortOrder) { CorrelationId = videoRequest.CorrelationId, Status = AudioStatusNames.AudioRequestCreated, CurrentStep = EventNames.AudioRequestCreated };

                await audioRequestRepository.InsertAsync(audioRequest, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.AudioRequestCreated,
                    reference: new
                    {
                        videoRequest.ScopeKey,
                        Type = nameof(AudioRequest),
                        Key = audioRequestId,
                        RefType = videoRequest.RefContentType.ToString(),
                        RefKey = videoRequest.RefContentId,
                        VideoRequestId = videoRequest.Id,
                        item.SortOrder
                    },
                    facility: Facilities.AudioRequestCreated,
                    correlationId: videoRequest.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: videoRequest.CorrelationId,
                    eventMessage: new AudioProviderRequestStartedEto { AudioRequestId = audioRequest.Id }
                );
            }
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoOperationStarted,
                Facilities.VideoOperationFailed,
                ex,
                cancellationToken
            );
        }
    }

    public async Task StartAudioProviderRequestAsync(AudioProviderRequestStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);
        var audioProvider = audioProviderResolver.Resolve(audioRequest.AudioProviderKey);

        try
        {
            audioRequest.Status = AudioStatusNames.AudioProviderRequestStarted;
            audioRequest.CurrentStep = EventNames.AudioProviderRequestStarted;

            long claimed = await ReplaceAudioAsync(audioRequest, cancellationToken,
                validPriorStatuses: [AudioStatusNames.AudioRequestCreated, AudioStatusNames.WaitingRetry, AudioStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.AudioProviderRequestStarted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = audioRequest.RefContentType.ToString(),
                    RefKey = audioRequest.RefContentId,
                    audioRequest.VideoRequestId,
                    audioRequest.SortOrder
                },
                facility: Facilities.AudioProviderRequestStarted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));

            var response = await audioProvider.CreateAsync(new AudioCreateRequest { AudioReferenceKey = audioRequest.Id.ToString("N").ToLower(), InputText = audioRequest.InputText });

            if (response.IsFailed)
                throw new ProcessException(response.ErrorMessage ?? "Audio provider create failed.", response.IsRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            audioRequest.AudioProviderTrackingId = response.ProviderTrackId;
            audioRequest.AudioProviderUrl = response.ProviderFileUrl;

            if (audioProvider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                audioRequest.Status = AudioStatusNames.AudioProviderCompleted;
                audioRequest.CurrentStep = EventNames.AudioProviderCompleted;

                await ReplaceAudioAsync(audioRequest, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.AudioProviderCompleted,
                    reference: new
                    {
                        audioRequest.ScopeKey,
                        Type = nameof(AudioRequest),
                        Key = audioRequest.Id,
                        RefType = audioRequest.RefContentType.ToString(),
                        RefKey = audioRequest.RefContentId,
                        audioRequest.VideoRequestId
                    },
                    facility: Facilities.AudioProviderRequestCompleted,
                    correlationId: audioRequest.CorrelationId,
                    exception: null
                ));

                // Publish provider completed event - handler will trigger download cascade
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: audioRequest.CorrelationId,
                    eventMessage: new AudioProviderCompletedEto { AudioRequestId = audioRequest.Id }
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Audio provider track id is required.");

            audioRequest.Status = AudioStatusNames.AudioProviderPolling;
            audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
            audioRequest.ProviderPollingCount = 0;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.AudioProviderPollingStarted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = audioRequest.RefContentType.ToString(),
                    RefKey = audioRequest.RefContentId,
                    audioRequest.VideoRequestId,
                    audioRequest.NextProviderPollAtUtc
                },
                facility: EventNames.AudioProviderPollingStarted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioProviderRequestStarted,
                Facilities.AudioOperationFailed,
                ex,
                cancellationToken
            );
        }
    }

    public async Task HandleAudioProviderCompletedAsync(AudioProviderCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.AudioFileDownloadStarted,
            reference: new
            {
                audioRequest.ScopeKey,
                Type = nameof(AudioRequest),
                Key = audioRequest.Id,
                RefType = audioRequest.RefContentType.ToString(),
                RefKey = audioRequest.RefContentId,
                audioRequest.VideoRequestId
            },
            facility: Facilities.AudioFileDownloadStarted,
            correlationId: audioRequest.CorrelationId,
            exception: null
        ));

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            correlationId: audioRequest.CorrelationId,
            eventMessage: new AudioFileDownloadStartedEto { AudioRequestId = audioRequest.Id }
        );
    }

    public async Task DownloadAudioFileAsync(AudioFileDownloadStartedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = AudioStatusNames.AudioFileDownloading;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;

            long claimed = await ReplaceAudioAsync(audioRequest, cancellationToken,
                validPriorStatuses: [AudioStatusNames.AudioProviderCompleted, AudioStatusNames.WaitingRetry, AudioStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            if (string.IsNullOrWhiteSpace(audioRequest.AudioProviderUrl))
                throw new InvalidOperationException("Audio provider url is required.");

            (bool success, string audioLocalPath, bool isRetryable) = await remoteFileDownloader.DownloadAsync(audioRequest.AudioProviderUrl);
            if (!success) throw new ProcessException($"Failed to download audio file: {audioLocalPath}", isRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            audioRequest.AudioLocalPath = audioLocalPath;

            audioRequest.Status = AudioStatusNames.AudioFileDownloadCompleted;
            audioRequest.CurrentStep = EventNames.AudioFileDownloadCompleted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.AudioFileDownloadCompleted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = audioRequest.RefContentType.ToString(),
                    RefKey = audioRequest.RefContentId,
                    audioRequest.VideoRequestId
                },
                facility: Facilities.AudioFileDownloadCompleted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: audioRequest.CorrelationId,
                eventMessage: new AudioFileDownloadCompletedEto { AudioRequestId = audioRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioFileDownloadStarted,
                Facilities.AudioOperationFailed,
                ex, cancellationToken);
        }
    }

    public async Task HandleAudioDownloadCompletedAsync(AudioFileDownloadCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        if (audioRequest.Status == AudioStatusNames.AudioFileUploadCompleted)
            return;

        try
        {
            audioRequest.Status = AudioStatusNames.AudioFileUploading;
            audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;

            long claimed = await ReplaceAudioAsync(audioRequest, cancellationToken,
                validPriorStatuses: [AudioStatusNames.AudioFileDownloadCompleted, AudioStatusNames.WaitingRetry, AudioStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.AudioFileUploadStarted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = audioRequest.RefContentType.ToString(),
                    RefKey = audioRequest.RefContentId,
                    audioRequest.VideoRequestId
                },
                facility: Facilities.AudioFileUploadStarted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));

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

            var uploadResult = await cdnProvider.UploadAsync(
                fileStream,
                fileName
            );

            if (uploadResult.IsFailed)
                throw new ProcessException(uploadResult.ErrorMessage ?? "CDN upload failed.", uploadResult.IsRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            audioRequest.AudioCdnProviderKey = systemCdnSettings.Selected;
            audioRequest.AudioCdnUrl = uploadResult.CdnUrl;
            audioRequest.AudioStorageUrl = uploadResult.StorageUrl;

            audioRequest.Status = AudioStatusNames.AudioFileUploadCompleted;
            audioRequest.CurrentStep = EventNames.AudioFileUploadCompleted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            // Only delete the local file once the CDN url is durably persisted — and clear the
            // now-dangling local path in the DB too, so the record never points at a deleted file.
            TryDeleteLocalFile(audioRequest.AudioLocalPath);
            audioRequest.AudioLocalPath = null;
            await ReplaceAudioAsync(audioRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.AudioFileUploadCompleted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = audioRequest.RefContentType.ToString(),
                    RefKey = audioRequest.RefContentId,
                    audioRequest.VideoRequestId,
                    audioRequest.AudioCdnUrl
                },
                facility: Facilities.AudioFileUploadCompleted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));

            // Fast path: don't wait for the next CheckReadyAudioRequestsToVideoAsync tick if this
            // was the last sibling audio to finish — the periodic tick remains the safety net.
            await videoOperationRetryWorkerService.CheckReadyAudioRequestsToVideoAsync(audioRequest.VideoRequestId, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                EventNames.AudioFileUploadStarted,
                Facilities.AudioOperationFailed,
                ex, cancellationToken);
        }
    }

    public async Task StartVideoProviderRequestAsync(VideoProviderRequestStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        var customerVpSetting = await customerVpSettingRepository.GetFirstOrDefaultAsync(x => x.ScopeKey == videoRequest.ScopeKey, cancellationToken: cancellationToken);
        if (customerVpSetting is null || string.IsNullOrWhiteSpace(customerVpSetting.VideoProviderKey))
        {
            throw new InvalidOperationException($"Provider key value is unknown. Scope key: {videoRequest.ScopeKey}");
        }

        var provider = videoProviderResolver.Resolve(customerVpSetting.VideoProviderKey);

        try
        {
            videoRequest.Status = VideoStatusNames.VideoProviderRequestStarted;
            videoRequest.CurrentStep = EventNames.VideoProviderRequestStarted;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoProviderRequestStarting, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            var response = await provider.CreateAsync(new VideoCreateRequest { VideoInputJson = videoRequest.MediaInputJson, AudioCdnUrls = @event.AudioCdnUrls, RefContentType = videoRequest.RefContentType, CustomerProviderSettings = customerVpSetting.VideoGenerationProviderSettings });

            if (response.IsFailed)
                throw new ProcessException(response.ErrorMessage ?? "Video provider create failed.", response.IsRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            videoRequest.VideoProviderTrackingId = response.ProviderTrackId;
            videoRequest.VideoProviderUrl = response.ProviderFileUrl;

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                videoRequest.Status = VideoStatusNames.VideoProviderCompleted;
                videoRequest.CurrentStep = EventNames.VideoProviderCompleted;

                await ReplaceVideoAsync(videoRequest, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.VideoProviderCompleted,
                    reference: new
                    {
                        videoRequest.ScopeKey,
                        Type = nameof(VideoRequest),
                        Key = videoRequest.Id,
                        RefType = videoRequest.RefContentType.ToString(),
                        RefKey = videoRequest.RefContentId
                    },
                    facility: Facilities.VideoProviderRequestCompleted,
                    correlationId: videoRequest.CorrelationId,
                    exception: null
                ));

                // Publish provider completed event - handler will trigger download cascade
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: videoRequest.CorrelationId,
                    eventMessage: new VideoProviderCompletedEto { VideoRequestId = videoRequest.Id }
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Video provider track id is required.");

            videoRequest.Status = VideoStatusNames.VideoProviderPolling;
            videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
            videoRequest.ProviderPollingCount = 0;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.VideoProviderPollingStarted,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = nameof(VideoRequest),
                    Key = videoRequest.Id,
                    RefType = videoRequest.RefContentType.ToString(),
                    RefKey = videoRequest.RefContentId,
                    videoRequest.NextProviderPollAtUtc
                },
                facility: EventNames.VideoProviderPollingStarted,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoProviderRequestStarted,
                Facilities.VideoOperationFailed,
                ex,
                cancellationToken
            );
        }
    }

    public async Task HandleVideoProviderCompletedAsync(VideoProviderCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: EventNames.VideoFileDownloadStarted,
            reference: new
            {
                videoRequest.ScopeKey,
                Type = nameof(VideoRequest),
                Key = videoRequest.Id,
                RefType = videoRequest.RefContentType.ToString(),
                RefKey = videoRequest.RefContentId
            },
            facility: Facilities.VideoFileDownloadStarted,
            correlationId: videoRequest.CorrelationId,
            exception: null
        ));

        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
            correlationId: videoRequest.CorrelationId,
            eventMessage: new VideoFileDownloadStartedEto { VideoRequestId = videoRequest.Id }
        );
    }

    public async Task DownloadVideoFileAsync(VideoFileDownloadStartedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = VideoStatusNames.VideoFileDownloading;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadStarted;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoProviderCompleted, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            (bool success, string videoLocalPath, bool isRetryable) = await remoteFileDownloader.DownloadAsync(videoRequest.VideoProviderUrl);
            if (!success) throw new ProcessException($"Failed to download video file: {videoLocalPath}", isRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            videoRequest.VideoLocalPath = videoLocalPath;

            videoRequest.Status = VideoStatusNames.VideoFileDownloaded;
            videoRequest.CurrentStep = EventNames.VideoFileDownloadCompleted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.VideoFileDownloadCompleted,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = nameof(VideoRequest),
                    Key = videoRequest.Id,
                    RefType = videoRequest.RefContentType.ToString(),
                    RefKey = videoRequest.RefContentId
                },
                facility: Facilities.VideoFileDownloadCompleted,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: videoRequest.CorrelationId,
                eventMessage: new VideoFileDownloadCompletedEto { VideoRequestId = videoRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileDownloadStarted,
                Facilities.VideoOperationFailed,
                ex, cancellationToken);
        }
    }

    public async Task HandleVideoDownloadCompletedAsync(VideoFileDownloadCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        if (videoRequest.Status == VideoStatusNames.VideoFileUploadCompleted || videoRequest.Status == VideoStatusNames.Completed)
        {
            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: videoRequest.CorrelationId,
                eventMessage: new VideoFileUploadCompletedEto { VideoRequestId = videoRequest.Id });
            return;
        }

        try
        {
            string cdnProviderKey = systemCdnSettings.Selected;

            videoRequest.Status = VideoStatusNames.VideoFileUploading;
            videoRequest.CurrentStep = EventNames.VideoFileUploadStarted;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoFileDownloaded, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.VideoFileUploadStarted,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = nameof(VideoRequest),
                    Key = videoRequest.Id,
                    RefType = videoRequest.RefContentType.ToString(),
                    RefKey = videoRequest.RefContentId
                },
                facility: Facilities.VideoFileUploadStarted,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            if (string.IsNullOrWhiteSpace(videoRequest.VideoLocalPath) || !File.Exists(videoRequest.VideoLocalPath))
                throw new InvalidOperationException("Video Local Path is required.");

            // Upload file to CDN using resolved CDN provider
            await using var fileStream = File.OpenRead(videoRequest.VideoLocalPath);
            string fileName = Path.GetFileName(videoRequest.VideoLocalPath);

            var cdnProvider = cdnProviderResolver.Resolve(cdnProviderKey);
            var uploadResult = await cdnProvider.UploadAsync(
                fileStream,
                fileName
            );

            if (uploadResult.IsFailed)
                throw new ProcessException(uploadResult.ErrorMessage ?? "CDN upload failed.", uploadResult.IsRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            videoRequest.VideoCdnProviderKey = cdnProviderKey;
            videoRequest.VideoCdnUrl = uploadResult.CdnUrl;
            videoRequest.VideoStorageUrl = uploadResult.StorageUrl;

            videoRequest.Status = VideoStatusNames.VideoFileUploadCompleted;
            videoRequest.CurrentStep = EventNames.VideoFileUploadCompleted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            // Only delete the local file once the CDN url is durably persisted — and clear the
            // now-dangling local path in the DB too, so the record never points at a deleted file.
            TryDeleteLocalFile(videoRequest.VideoLocalPath);
            videoRequest.VideoLocalPath = null;
            await ReplaceVideoAsync(videoRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.VideoFileUploadCompleted,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = nameof(VideoRequest),
                    Key = videoRequest.Id,
                    RefType = videoRequest.RefContentType.ToString(),
                    RefKey = videoRequest.RefContentId,
                    videoRequest.VideoCdnUrl
                },
                facility: Facilities.VideoFileUploadCompleted,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: videoRequest.CorrelationId,
                eventMessage: new VideoFileUploadCompletedEto { VideoRequestId = videoRequest.Id }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileUploadStarted,
                Facilities.VideoOperationFailed,
                ex, cancellationToken);
        }
    }

    public async Task HandleVideoUploadCompletedAsync(VideoFileUploadCompletedEto @event, CancellationToken cancellationToken = default)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = VideoStatusNames.Completed;
            videoRequest.CurrentStep = EventNames.VideoGenerationResultPublished;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoFileUploadCompleted, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: EventNames.VideoGenerationResultPublished,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = nameof(VideoRequest),
                    Key = videoRequest.Id,
                    RefType = videoRequest.RefContentType.ToString(),
                    RefKey = videoRequest.RefContentId,
                    videoRequest.VideoCdnUrl
                },
                facility: Facilities.VideoGenerationResultPublished,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: videoRequest.CorrelationId,
                eventMessage: new VideoGenerationResultPublishedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, FinalVideoUrl = videoRequest.VideoCdnUrl }
            );
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                EventNames.VideoFileUploadCompleted,
                Facilities.VideoOperationFailed,
                ex, cancellationToken);
        }
    }

    #region Private methods

    private static List<VideoInputAudioItem> ExtractAudioItems(string videoInputJson)
    {
        using var doc = JsonDocument.Parse(videoInputJson);

        return doc.RootElement
            .GetProperty("audioItems")
            .EnumerateArray()
            .Select(x => new VideoInputAudioItem { SortOrder = x.GetProperty("sortOrder").GetInt32(), Text = StringHelper.Base64Decode(x.GetProperty("encodedOutlineData").GetString() ?? string.Empty) })
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

    private Task<long> ReplaceVideoAsync(VideoRequest request, CancellationToken cancellationToken = default, IReadOnlyCollection<string> validPriorStatuses = null)
    {
        Expression<Func<VideoRequest, bool>> predicate = validPriorStatuses is null
            ? x => x.Id == request.Id
            : x => x.Id == request.Id && validPriorStatuses.Contains(x.Status);
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

        return videoRequestRepository.UpdateByExpressionAsync(predicate, _ => update, cancellationToken: cancellationToken);
    }

    private Task<long> ReplaceAudioAsync(AudioRequest request, CancellationToken cancellationToken = default, IReadOnlyCollection<string> validPriorStatuses = null)
    {
        Expression<Func<AudioRequest, bool>> predicate = validPriorStatuses is null
            ? x => x.Id == request.Id
            : x => x.Id == request.Id && validPriorStatuses.Contains(x.Status);
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

        return audioRequestRepository.UpdateByExpressionAsync(predicate, _ => update, cancellationToken: cancellationToken);
    }

    private async Task HandleAudioExceptionAsync(AudioRequest request, string step, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAudioRetryAsync(request, step, facility, ex, cancellationToken);
            return;
        }

        await FailAudioAsync(request, step, facility, ex, false, cancellationToken);
    }

    private async Task ScheduleAudioRetryAsync(AudioRequest request, string step, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        request.RetryCount++;

        if (request.RetryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailAudioAsync(request, step, facility, ex, false, cancellationToken);
            return;
        }

        request.Status = AudioStatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await ReplaceAudioAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: EventNames.RetryScheduled,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(AudioRequest),
                Key = request.Id,
                RefType = request.RefContentType.ToString(),
                RefKey = request.RefContentId,
                request.VideoRequestId,
                FailedStep = step,
                request.RetryCount,
                request.NextRetryAtUtc
            },
            facility: Facilities.RetryScheduled,
            correlationId: request.CorrelationId,
            exception: ex
        ));

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

    private async Task FailAudioAsync(AudioRequest request, string step, string facility, Exception ex, bool retryable, CancellationToken cancellationToken = default)
    {
        request.Status = AudioStatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        // Terminal failure — this request will never be retried, so the local file is dead
        // weight; delete it and clear the path so the DB record doesn't reference a missing file.
        TryDeleteLocalFile(request.AudioLocalPath);
        request.AudioLocalPath = null;

        await ReplaceAudioAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: step,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(AudioRequest),
                Key = request.Id,
                RefType = request.RefContentType.ToString(),
                RefKey = request.RefContentId,
                request.VideoRequestId,
                FailedStep = step,
                retryable
            },
            facility: facility,
            correlationId: request.CorrelationId,
            exception: ex
        ));

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
        if (parentVideoRequest != null && parentVideoRequest.Status != VideoStatusNames.Failed && parentVideoRequest.Status != VideoStatusNames.Completed)
        {
            await FailVideoRequestDueToAudioFailureAsync(parentVideoRequest, facility, cancellationToken);
        }
    }

    private async Task FailVideoRequestDueToAudioFailureAsync(VideoRequest videoRequest, string facility, CancellationToken cancellationToken = default)
    {
        videoRequest.Status = VideoStatusNames.Failed;
        videoRequest.CurrentStep = EventNames.AudioFileUploadCompleted;
        videoRequest.LastError = "One or more audio requests failed.";
        videoRequest.NextRetryAtUtc = null;

        await ReplaceVideoAsync(videoRequest, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: EventNames.AudioFileUploadCompleted,
            reference: new
            {
                videoRequest.ScopeKey,
                Type = nameof(VideoRequest),
                Key = videoRequest.Id,
                RefType = videoRequest.RefContentType.ToString(),
                RefKey = videoRequest.RefContentId
            },
            facility: facility,
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
    }

    private async Task HandleVideoExceptionAsync(VideoRequest request, string step, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleVideoRetryAsync(request, step, facility, ex, cancellationToken);
            return;
        }

        await FailVideoAsync(request, step, facility, ex, false, cancellationToken);
    }

    private async Task ScheduleVideoRetryAsync(VideoRequest request, string step, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        request.RetryCount++;

        if (request.RetryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailVideoAsync(request, step, facility, ex, false, cancellationToken);
            return;
        }

        request.Status = VideoStatusNames.WaitingRetry;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await ReplaceVideoAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: EventNames.RetryScheduled,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(VideoRequest),
                Key = request.Id,
                RefType = request.RefContentType.ToString(),
                RefKey = request.RefContentId,
                FailedStep = step,
                request.RetryCount,
                request.NextRetryAtUtc
            },
            facility: Facilities.RetryScheduled,
            correlationId: request.CorrelationId,
            exception: ex
        ));

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

    private async Task FailVideoAsync(VideoRequest request, string step, string facility, Exception ex, bool retryable, CancellationToken cancellationToken = default)
    {
        request.Status = VideoStatusNames.Failed;
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        // Terminal failure — this request will never be retried, so the local file is dead
        // weight; delete it and clear the path so the DB record doesn't reference a missing file.
        TryDeleteLocalFile(request.VideoLocalPath);
        request.VideoLocalPath = null;

        await ReplaceVideoAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: step,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(VideoRequest),
                Key = request.Id,
                RefType = request.RefContentType.ToString(),
                RefKey = request.RefContentId,
                FailedStep = step,
                retryable
            },
            facility: facility,
            correlationId: request.CorrelationId,
            exception: ex
        ));

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

    private static void TryDeleteLocalFile(string localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath)) return;
        try
        {
            File.Delete(localPath);
        }
        catch
        {
            /* best-effort: log or ignore */
        }
    }

    #endregion
}

public sealed class VideoInputAudioItem
{
    public int SortOrder { get; init; }
    public string Text { get; init; } = string.Empty;
}