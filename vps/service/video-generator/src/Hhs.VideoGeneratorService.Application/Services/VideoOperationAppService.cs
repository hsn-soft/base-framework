using System.Linq.Expressions;
using System.Text.Json;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Application.Providers.Audio;
using Hhs.VideoGeneratorService.Application.Providers.FileDownloader;
using Hhs.VideoGeneratorService.Application.Providers.Video;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.Constants;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.Settings;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    VideoGenerationSettings videoGenerationSettings,
    VideoOperationRetryWorkerService videoOperationRetryWorkerService) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();
    private readonly IHostEnvironment _environment = provider.GetRequiredService<IHostEnvironment>();

    // Development-only bypass (VideoGenerationSettings:Skip*GenerationOperation): writes a small
    // placeholder file and hands its local path back as if a real provider had already produced
    // and hosted the file — the rest of the pipeline (download short-circuits on a local path,
    // then CDN upload, then completion) runs completely unchanged.
    private async Task<string> CreateDummyProviderFileAsync(string fileName)
    {
        string downloadDir = systemCdnSettings.LocalDownloadPath;
        if (_environment.IsDevelopment())
        {
            downloadDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", downloadDir);
        }

        Directory.CreateDirectory(downloadDir);
        string filePath = Path.Combine(downloadDir, "dummy_media_"+fileName);

        await File.WriteAllTextAsync(filePath, $"Dummy generated file (SkipGenerationOperation=true)\nCreated: {DateTime.UtcNow:O}");

        return filePath;
    }

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
            throw new InvalidOperationException($"{ErrorMessages.ProviderKeyValueUnknown} {@event.ScopeKey}");
        }

        var videoProvider = videoProviderResolver.Resolve(videoProviderKeyResult.Value);

        string audioProviderKey = null;
        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired)
        {
            var audioProviderKeyResult = await customerVpSettingRepository.GetAudioProviderKeyByScopeKeyAsync(@event.ScopeKey, cancellationToken);
            if (!audioProviderKeyResult.Key)
            {
                throw new InvalidOperationException($"{ErrorMessages.ProviderKeyValueUnknown} {@event.ScopeKey}");
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
            CurrentMilestone = Milestones.VideoRequestCreated,
            MediaInputJson = @event.VideoInputJson,
            AudioProviderKey = audioProviderKey,
            VideoProviderKey = videoProviderKeyResult.Value
        };

        await videoRequestRepository.InsertAsync(videoRequest, cancellationToken);

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: Milestones.VideoRequestCreated,
            reference: new
            {
                @event.ScopeKey,
                Type = @event.RefContentType.ToString(),
                Key = @event.RefContentId,
                RefType = nameof(VideoRequest),
                RefKey = videoRequestId
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
        videoRequest.CurrentMilestone = Milestones.VideoOperationStarted;

        long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
            validPriorStatuses: [VideoStatusNames.Created, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
        if (claimed == 0) return;

        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: Milestones.VideoOperationStarted,
            reference: new
            {
                videoRequest.ScopeKey,
                Type = videoRequest.RefContentType.ToString(),
                Key = videoRequest.RefContentId,
                RefType = nameof(VideoRequest),
                RefKey = videoRequest.Id,
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
                    message: Milestones.VideoAudioInternal,
                    reference: new
                    {
                        videoRequest.ScopeKey,
                        Type = videoRequest.RefContentType.ToString(),
                        Key = videoRequest.RefContentId,
                        RefType = nameof(VideoRequest),
                        RefKey = videoRequest.Id,
                    },
                    facility: Facilities.VideoAudioInternal,
                    correlationId: videoRequest.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: videoRequest.CorrelationId,
                    eventMessage: new AudioOperationStartedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, AudioMode = Milestones.VideoAudioInternal }
                );

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: videoRequest.CorrelationId,
                    eventMessage: new VideoProviderRequestStartedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, AudioCdnUrls = [] }
                );

                return;
            }

            if (videoRequest.AudioProviderKey is null) throw new InvalidOperationException(ErrorMessages.AudioProviderKeyRequired);

            var audioProvider = audioProviderResolver.Resolve(videoRequest.AudioProviderKey);

            var audioItems = ExtractAudioItems(videoRequest.MediaInputJson);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.VideoAudioExternal,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = videoRequest.RefContentType.ToString(),
                    Key = videoRequest.RefContentId,
                    RefType = nameof(VideoRequest),
                    RefKey = videoRequest.Id,
                    AudioCount = audioItems.Count
                },
                facility: Facilities.VideoAudioExternal,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));

            await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                correlationId: videoRequest.CorrelationId,
                eventMessage: new AudioOperationStartedEto { RefContentId = videoRequest.RefContentId, RefContentType = videoRequest.RefContentType, VideoRequestId = videoRequest.Id, AudioMode = Milestones.VideoAudioExternal }
            );

            foreach (var item in audioItems)
            {
                var audioOptions = new ListQueryOptions<AudioRequest> { Filter = x => x.VideoRequestId == videoRequest.Id && x.SortOrder == item.SortOrder };
                var existingAudio = (await audioRequestRepository.GetListAsync(audioOptions, cancellationToken)).FirstOrDefault();

                if (existingAudio is not null)
                {
                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: Milestones.AudioProviderRequestStarted,
                        reference: new
                        {
                            videoRequest.ScopeKey,
                            Type = nameof(AudioRequest),
                            Key = existingAudio.Id,
                            RefType = "CustomerContent",
                            RefKey = existingAudio.CustomerContentIdForItem,
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
                    item.CustomerContentId,
                    videoRequest.ScopeKey,
                    eventId,
                    item.Text,
                    videoRequest.AudioProviderKey,
                    item.SortOrder) { CorrelationId = videoRequest.CorrelationId, Status = AudioStatusNames.AudioRequestCreated, CurrentMilestone = Milestones.AudioRequestCreated };

                await audioRequestRepository.InsertAsync(audioRequest, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: Milestones.AudioRequestCreated,
                    reference: new
                    {
                        videoRequest.ScopeKey,
                        Type = nameof(AudioRequest),
                        Key = audioRequestId,
                        RefType = "CustomerContent",
                        RefKey = audioRequest.CustomerContentIdForItem,
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
                Milestones.VideoOperationStarted,
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
            audioRequest.CurrentMilestone = Milestones.AudioProviderRequestStarted;

            long claimed = await ReplaceAudioAsync(audioRequest, cancellationToken,
                validPriorStatuses: [AudioStatusNames.AudioRequestCreated, AudioStatusNames.WaitingRetry, AudioStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.AudioProviderRequestStarted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = "CustomerContent",
                    RefKey = audioRequest.CustomerContentIdForItem,
                    audioRequest.VideoRequestId,
                    audioRequest.SortOrder
                },
                facility: Facilities.AudioProviderRequestStarted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));

            AudioCreateResponse response;
            if (videoGenerationSettings.SkipAudioGenerationOperation)
            {
                string dummyFilePath = await CreateDummyProviderFileAsync($"{audioRequest.Id:N}.mp3");
                response = new AudioCreateResponse { IsCompleted = true, ProviderFileUrl = dummyFilePath };
            }
            else
            {
                response = await audioProvider.CreateAsync(new AudioCreateRequest { AudioReferenceKey = audioRequest.Id.ToString("N").ToLower(), InputText = audioRequest.InputText });

                if (response.IsFailed)
                    throw new ProcessException(response.ErrorMessage ?? "Audio provider create failed.", response.IsRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);
            }

            audioRequest.AudioProviderTrackingId = response.ProviderTrackId;
            audioRequest.AudioProviderUrl = response.ProviderFileUrl;

            if (videoGenerationSettings.SkipAudioGenerationOperation || audioProvider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException(ErrorMessages.AudioProviderFailedNoUrl);

                audioRequest.Status = AudioStatusNames.AudioProviderCompleted;
                audioRequest.CurrentMilestone = Milestones.AudioProviderCompleted;

                await ReplaceAudioAsync(audioRequest, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: Milestones.AudioProviderCompleted,
                    reference: new
                    {
                        audioRequest.ScopeKey,
                        Type = nameof(AudioRequest),
                        Key = audioRequest.Id,
                        RefType = "CustomerContent",
                        RefKey = audioRequest.CustomerContentIdForItem,
                        audioRequest.VideoRequestId,
                        audioRequest.SortOrder
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
                throw new InvalidOperationException(ErrorMessages.AudioProviderTrackIdRequired);

            audioRequest.Status = AudioStatusNames.AudioProviderPolling;
            audioRequest.CurrentMilestone = Milestones.AudioProviderPollingStarted;
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
            audioRequest.ProviderPollingCount = 0;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.AudioProviderPollingStarted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = "CustomerContent",
                    RefKey = audioRequest.CustomerContentIdForItem,
                    audioRequest.VideoRequestId,
                    audioRequest.NextProviderPollAtUtc,
                    audioRequest.SortOrder
                },
                facility: Facilities.AudioProviderPollingStarted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));
        }
        catch (Exception ex)
        {
            await HandleAudioExceptionAsync(
                audioRequest,
                Milestones.AudioProviderRequestStarted,
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
            message: Milestones.AudioFileDownloadStarted,
            reference: new
            {
                audioRequest.ScopeKey,
                Type = nameof(AudioRequest),
                Key = audioRequest.Id,
                RefType = "CustomerContent",
                RefKey = audioRequest.CustomerContentIdForItem,
                audioRequest.VideoRequestId,
                audioRequest.SortOrder
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
            audioRequest.CurrentMilestone = Milestones.AudioFileDownloadStarted;

            long claimed = await ReplaceAudioAsync(audioRequest, cancellationToken,
                validPriorStatuses: [AudioStatusNames.AudioProviderCompleted, AudioStatusNames.WaitingRetry, AudioStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            if (string.IsNullOrWhiteSpace(audioRequest.AudioProviderUrl))
                throw new InvalidOperationException(ErrorMessages.AudioProviderUrlRequired);

            (bool success, string audioLocalPath, bool isRetryable) = await remoteFileDownloader.DownloadAsync(audioRequest.AudioProviderUrl);
            if (!success) throw new ProcessException($"Failed to download audio file: {audioLocalPath}", isRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            audioRequest.AudioLocalPath = audioLocalPath;

            audioRequest.Status = AudioStatusNames.AudioFileDownloadCompleted;
            audioRequest.CurrentMilestone = Milestones.AudioFileDownloadCompleted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.AudioFileDownloadCompleted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = "CustomerContent",
                    RefKey = audioRequest.CustomerContentIdForItem,
                    audioRequest.VideoRequestId,
                    audioRequest.SortOrder
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
                Milestones.AudioFileDownloadStarted,
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
            audioRequest.CurrentMilestone = Milestones.AudioFileUploadStarted;

            long claimed = await ReplaceAudioAsync(audioRequest, cancellationToken,
                validPriorStatuses: [AudioStatusNames.AudioFileDownloadCompleted, AudioStatusNames.WaitingRetry, AudioStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.AudioFileUploadStarted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = "CustomerContent",
                    RefKey = audioRequest.CustomerContentIdForItem,
                    audioRequest.VideoRequestId,
                    audioRequest.SortOrder
                },
                facility: Facilities.AudioFileUploadStarted,
                correlationId: audioRequest.CorrelationId,
                exception: null
            ));

            if (string.IsNullOrWhiteSpace(audioRequest.AudioLocalPath))
                throw new InvalidOperationException(ErrorMessages.AudioLocalPathRequired);

            if (!File.Exists(audioRequest.AudioLocalPath))
                throw new InvalidOperationException($"{ErrorMessages.AudioLocalFileNotExist} {audioRequest.AudioLocalPath}");

            // Upload file to CDN using resolved CDN provider
            await using var fileStream = File.OpenRead(audioRequest.AudioLocalPath);
            string fileName = Path.GetFileName(audioRequest.AudioLocalPath);

            if (string.IsNullOrWhiteSpace(systemCdnSettings.Selected))
                throw new InvalidOperationException(ErrorMessages.CdnProviderKeyRequired);

            var cdnProvider = cdnProviderResolver.Resolve(systemCdnSettings.Selected);

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
            audioRequest.CurrentMilestone = Milestones.AudioFileUploadCompleted;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            // Only delete the local file once the CDN url is durably persisted — and clear the
            // now-dangling local path in the DB too, so the record never points at a deleted file.
            if (!videoGenerationSettings.SkipLocalMediaFilesCleanup)
            {
                TryDeleteLocalFile(audioRequest.AudioLocalPath);
                audioRequest.AudioLocalPath = null;
                await ReplaceAudioAsync(audioRequest, cancellationToken);
            }

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.AudioFileUploadCompleted,
                reference: new
                {
                    audioRequest.ScopeKey,
                    Type = nameof(AudioRequest),
                    Key = audioRequest.Id,
                    RefType = "CustomerContent",
                    RefKey = audioRequest.CustomerContentIdForItem,
                    audioRequest.VideoRequestId,
                    audioRequest.AudioCdnUrl,
                    audioRequest.SortOrder
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
                Milestones.AudioFileUploadStarted,
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
            throw new InvalidOperationException($"{ErrorMessages.ProviderKeyValueUnknown} {videoRequest.ScopeKey}");
        }

        var provider = videoProviderResolver.Resolve(customerVpSetting.VideoProviderKey);

        try
        {
            videoRequest.Status = VideoStatusNames.VideoProviderRequestStarted;
            videoRequest.CurrentMilestone = Milestones.VideoProviderRequestStarted;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoProviderRequestStarting, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            VideoCreateResponse response;
            if (videoGenerationSettings.SkipVideoGenerationOperation)
            {
                string dummyFilePath = await CreateDummyProviderFileAsync($"{videoRequest.Id:N}.mp4");
                response = new VideoCreateResponse { IsCompleted = true, ProviderFileUrl = dummyFilePath };
            }
            else
            {
                response = await provider.CreateAsync(new VideoCreateRequest { VideoInputJson = videoRequest.MediaInputJson, AudioCdnUrls = @event.AudioCdnUrls, RefContentType = videoRequest.RefContentType, CustomerProviderSettings = customerVpSetting.VideoGenerationProviderSettings });

                if (response.IsFailed)
                    throw new ProcessException(response.ErrorMessage ?? "Video provider create failed.", response.IsRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);
            }

            videoRequest.VideoProviderTrackingId = response.ProviderTrackId;
            videoRequest.VideoProviderUrl = response.ProviderFileUrl;

            if (videoGenerationSettings.SkipVideoGenerationOperation || provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException(ErrorMessages.VideoProviderFailedNoUrl);

                videoRequest.Status = VideoStatusNames.VideoProviderCompleted;
                videoRequest.CurrentMilestone = Milestones.VideoProviderCompleted;

                await ReplaceVideoAsync(videoRequest, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: Milestones.VideoProviderCompleted,
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
                throw new InvalidOperationException(ErrorMessages.VideoProviderTrackIdRequired);

            videoRequest.Status = VideoStatusNames.VideoProviderPolling;
            videoRequest.CurrentMilestone = Milestones.VideoProviderPollingStarted;
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(videoPollingSettings.ErrorRescheduleDelaySeconds);
            videoRequest.ProviderPollingCount = 0;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.VideoProviderPollingStarted,
                reference: new
                {
                    videoRequest.ScopeKey,
                    Type = nameof(VideoRequest),
                    Key = videoRequest.Id,
                    RefType = videoRequest.RefContentType.ToString(),
                    RefKey = videoRequest.RefContentId,
                    videoRequest.NextProviderPollAtUtc
                },
                facility: Facilities.VideoProviderPollingStarted,
                correlationId: videoRequest.CorrelationId,
                exception: null
            ));
        }
        catch (Exception ex)
        {
            await HandleVideoExceptionAsync(
                videoRequest,
                Milestones.VideoProviderRequestStarted,
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
            message: Milestones.VideoFileDownloadStarted,
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
            videoRequest.CurrentMilestone = Milestones.VideoFileDownloadStarted;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoProviderCompleted, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            (bool success, string videoLocalPath, bool isRetryable) = await remoteFileDownloader.DownloadAsync(videoRequest.VideoProviderUrl);
            if (!success) throw new ProcessException($"Failed to download video file: {videoLocalPath}", isRetryable ? ProcessErrorType.Retryable : ProcessErrorType.NonRetryable);

            videoRequest.VideoLocalPath = videoLocalPath;

            videoRequest.Status = VideoStatusNames.VideoFileDownloaded;
            videoRequest.CurrentMilestone = Milestones.VideoFileDownloadCompleted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.VideoFileDownloadCompleted,
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
                Milestones.VideoFileDownloadStarted,
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
            videoRequest.CurrentMilestone = Milestones.VideoFileUploadStarted;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoFileDownloaded, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.VideoFileUploadStarted,
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
                throw new InvalidOperationException(ErrorMessages.VideoLocalPathRequired);

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
            videoRequest.CurrentMilestone = Milestones.VideoFileUploadCompleted;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            // Only delete the local file once the CDN url is durably persisted — and clear the
            // now-dangling local path in the DB too, so the record never points at a deleted file.
            if (!videoGenerationSettings.SkipLocalMediaFilesCleanup)
            {
                TryDeleteLocalFile(videoRequest.VideoLocalPath);
                videoRequest.VideoLocalPath = null;
                await ReplaceVideoAsync(videoRequest, cancellationToken);
            }

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.VideoFileUploadCompleted,
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
                Milestones.VideoFileUploadStarted,
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
            videoRequest.CurrentMilestone = Milestones.VideoGenerationResultPublished;

            long claimed = await ReplaceVideoAsync(videoRequest, cancellationToken,
                validPriorStatuses: [VideoStatusNames.VideoFileUploadCompleted, VideoStatusNames.WaitingRetry, VideoStatusNames.RetryEventPublished]);
            if (claimed == 0) return;

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: Milestones.VideoGenerationResultPublished,
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
                Milestones.VideoFileUploadCompleted,
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
            .Select(x => new VideoInputAudioItem { SortOrder = x.GetProperty("sortOrder").GetInt32(), Text = StringHelper.Base64Decode(x.GetProperty("encodedOutlineData").GetString() ?? string.Empty), CustomerContentId = x.GetProperty("customerContentId").GetGuid() })
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
            .Set(x => x.CurrentMilestone, request.CurrentMilestone)
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
            .Set(x => x.CurrentMilestone, request.CurrentMilestone)
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

    private async Task HandleAudioExceptionAsync(AudioRequest request, string milestone, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleAudioRetryAsync(request, milestone, facility, ex, cancellationToken);
            return;
        }

        await FailAudioAsync(request, milestone, facility, ex, false, cancellationToken);
    }

    private async Task ScheduleAudioRetryAsync(AudioRequest request, string milestone, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        request.RetryCount++;

        if (request.RetryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailAudioAsync(request, milestone, facility, ex, false, cancellationToken);
            return;
        }

        request.Status = AudioStatusNames.WaitingRetry;
        request.CurrentMilestone = milestone;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await ReplaceAudioAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: Milestones.RetryScheduled,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(AudioRequest),
                Key = request.Id,
                RefType = "CustomerContent",
                RefKey = request.CustomerContentIdForItem,
                request.VideoRequestId,
                request.SortOrder,
                FailedMilestone = milestone,
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
            eventMessage: new MilestoneFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Milestone = milestone,
                ErrorMessage = ex.Message,
                Retryable = true
            }
        );
    }

    private async Task FailAudioAsync(AudioRequest request, string milestone, string facility, Exception ex, bool retryable, CancellationToken cancellationToken = default)
    {
        request.Status = AudioStatusNames.Failed;
        request.CurrentMilestone = milestone;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        // Terminal failure — this request will never be retried, so the local file is dead
        // weight; delete it and clear the path so the DB record doesn't reference a missing file.
        if (!videoGenerationSettings.SkipLocalMediaFilesCleanup)
        {
            TryDeleteLocalFile(request.AudioLocalPath);
            request.AudioLocalPath = null;
        }

        await ReplaceAudioAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: milestone,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(AudioRequest),
                Key = request.Id,
                RefType = "CustomerContent",
                RefKey = request.CustomerContentIdForItem,
                request.VideoRequestId,
                request.SortOrder,
                FailedMilestone = milestone,
                retryable
            },
            facility: facility,
            correlationId: request.CorrelationId,
            exception: ex
        ));

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new MilestoneFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Milestone = milestone,
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
        videoRequest.CurrentMilestone = Milestones.AudioFileUploadCompleted;
        videoRequest.LastError = "One or more audio requests failed.";
        videoRequest.NextRetryAtUtc = null;

        await ReplaceVideoAsync(videoRequest, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: Milestones.AudioFileUploadCompleted,
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
            eventMessage: new MilestoneFailedEto
            {
                RefContentId = videoRequest.RefContentId,
                RefContentType = videoRequest.RefContentType,
                Milestone = Milestones.AudioFileUploadCompleted,
                ErrorMessage = "One or more audio requests failed.",
                Retryable = false
            }
        );
    }

    private async Task HandleVideoExceptionAsync(VideoRequest request, string milestone, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        if (ExceptionClassifier.IsRetryable(ex))
        {
            await ScheduleVideoRetryAsync(request, milestone, facility, ex, cancellationToken);
            return;
        }

        await FailVideoAsync(request, milestone, facility, ex, false, cancellationToken);
    }

    private async Task ScheduleVideoRetryAsync(VideoRequest request, string milestone, string facility, Exception ex, CancellationToken cancellationToken = default)
    {
        request.RetryCount++;

        if (request.RetryCount >= serviceRetrySettings.MaxRetryCount)
        {
            await FailVideoAsync(request, milestone, facility, ex, false, cancellationToken);
            return;
        }

        request.Status = VideoStatusNames.WaitingRetry;
        request.CurrentMilestone = milestone;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            retryDelayCalculator.Calculate(request.RetryCount));

        await ReplaceVideoAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: Milestones.RetryScheduled,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(VideoRequest),
                Key = request.Id,
                RefType = request.RefContentType.ToString(),
                RefKey = request.RefContentId,
                FailedMilestone = milestone,
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
            eventMessage: new MilestoneFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Milestone = milestone,
                ErrorMessage = ex.Message,
                Retryable = true
            }
        );
    }

    private async Task FailVideoAsync(VideoRequest request, string milestone, string facility, Exception ex, bool retryable, CancellationToken cancellationToken = default)
    {
        request.Status = VideoStatusNames.Failed;
        request.CurrentMilestone = milestone;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;

        // Terminal failure — this request will never be retried, so the local file is dead
        // weight; delete it and clear the path so the DB record doesn't reference a missing file.
        if (!videoGenerationSettings.SkipLocalMediaFilesCleanup)
        {
            TryDeleteLocalFile(request.VideoLocalPath);
            request.VideoLocalPath = null;
        }

        await ReplaceVideoAsync(request, cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: milestone,
            reference: new
            {
                request.ScopeKey,
                Type = nameof(VideoRequest),
                Key = request.Id,
                RefType = request.RefContentType.ToString(),
                RefKey = request.RefContentId,
                FailedMilestone = milestone,
                retryable
            },
            facility: facility,
            correlationId: request.CorrelationId,
            exception: ex
        ));

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new MilestoneFailedEto
            {
                RefContentId = request.RefContentId,
                RefContentType = request.RefContentType,
                Milestone = milestone,
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
    public Guid CustomerContentId { get; init; }
}