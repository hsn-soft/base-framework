using System.Text.Json;
using Hhs.Shared.Events;
using Hhs.Shared.Providers;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Retry;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
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
    RetryDelayCalculator retryDelayCalculator)
{
    private readonly RetryDelayCalculator _retryDelayCalculator = retryDelayCalculator;
public async Task CreateVideoRequestAsync(
    VideoGenerationApprovedEto @event,
    CancellationToken cancellationToken)
{
    var existing = await context.VideoRequests
        .Find(x => x.SourceEventId == @event.EventId)
        .FirstOrDefaultAsync(cancellationToken);

    if (existing is not null)
    {
        await eventBus.PublishAsync(new VideoRequestCreatedEto
        {
            CustomerContentId = existing.CustomerContentId,
            AnalysisContentId = existing.AnalysisContentId,
            ContentProcessType = existing.ContentProcessType,
            CorrelationId = existing.CorrelationId,
            VideoRequestId = existing.Id,
            IsAnalysis = existing.AnalysisContentId.HasValue,
            ExternalAudioRequired = existing.ExternalAudioRequired
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
        CustomerContentId = @event.CustomerContentId,
        AnalysisContentId = @event.AnalysisContentId,
        ContentProcessType = @event.ContentProcessType,
        Status = "CREATED",
        CurrentStep = EventNames.VideoRequestCreated,
        VideoInputJson = @event.VideoInputJson,
        VideoProviderKey = @event.VideoProviderKey,
        AudioProviderKey = @event.AudioProviderKey,
        AudioInputMode = videoProvider.Capabilities.AudioInputMode.ToString(),
        ExternalAudioRequired =
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired ||
            videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    await context.VideoRequests.InsertOneAsync(videoRequest, cancellationToken: cancellationToken);

    await eventBus.PublishAsync(new VideoRequestCreatedEto
    {
        CustomerContentId = @event.CustomerContentId,
        AnalysisContentId = @event.AnalysisContentId,
        ContentProcessType = @event.ContentProcessType,
        CorrelationId = @event.CorrelationId,
        VideoRequestId = videoRequestId,
        IsAnalysis = @event.AnalysisContentId.HasValue,
        ExternalAudioRequired = videoRequest.ExternalAudioRequired
    }, cancellationToken);
}

    public async Task StartVideoOperationAsync(VideoRequestCreatedEto @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        videoRequest.Status = "STARTED";
        videoRequest.CurrentStep = EventNames.VideoOperationStarted;
        videoRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(videoRequest, cancellationToken);

        await eventBus.PublishAsync(new VideoOperationStartedEto
        {
            CustomerContentId = videoRequest.CustomerContentId,
            AnalysisContentId = videoRequest.AnalysisContentId,
            ContentProcessType = videoRequest.ContentProcessType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = videoRequest.Id,
            ExternalAudioRequired = videoRequest.ExternalAudioRequired
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
            await eventBus.PublishAsync(new VideoProviderRequestStartedEto
            {
                CustomerContentId = videoRequest.CustomerContentId,
                AnalysisContentId = videoRequest.AnalysisContentId,
                ContentProcessType = videoRequest.ContentProcessType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                AudioUrls = []
            }, cancellationToken);

            return;
        }

        var audioItems = ExtractAudioItems(videoRequest.VideoInputJson);

        foreach (var item in audioItems)
        {
            var existingAudio = await context.AudioRequests
                .Find(x => x.VideoRequestId == videoRequest.Id && x.SortOrder == item.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAudio is not null)
            {
                await eventBus.PublishAsync(new AudioProviderRequestStartedEto
                {
                    CustomerContentId = existingAudio.CustomerContentId,
                    AnalysisContentId = existingAudio.AnalysisContentId,
                    ContentProcessType = existingAudio.ContentProcessType,
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
                CustomerContentId = item.CustomerContentId ?? videoRequest.CustomerContentId,
                AnalysisContentId = videoRequest.AnalysisContentId,
                ContentProcessType = videoRequest.ContentProcessType,
                SortOrder = item.SortOrder,
                InputText = item.Text,
                AudioProviderKey = videoRequest.AudioProviderKey
                                   ?? throw new InvalidOperationException("AudioProviderKey is required."),
                Status = "CREATED",
                CurrentStep = "AUDIO_REQUEST_CREATED",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await context.AudioRequests.InsertOneAsync(audioRequest, cancellationToken: cancellationToken);

            await eventBus.PublishAsync(new AudioProviderRequestStartedEto
            {
                CustomerContentId = audioRequest.CustomerContentId,
                AnalysisContentId = audioRequest.AnalysisContentId,
                ContentProcessType = videoRequest.ContentProcessType,
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
        var provider = audioProviderResolver.Resolve(audioRequest.AudioProviderKey);

        try
        {
            audioRequest.Status = "AUDIO_PROVIDER_REQUEST_STARTED";
            audioRequest.CurrentStep = EventNames.AudioProviderRequestStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var response = await provider.CreateAsync(new AudioCreateRequest { InputText = audioRequest.InputText }, cancellationToken);

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                audioRequest.ProviderFileName = response.FileName;

                // Download file from provider and upload to mock storage
                var localFileName = !string.IsNullOrWhiteSpace(response.FileName)
                    ? $"local_{response.FileName}"
                    : $"local_audio_{audioRequest.Id:N}.mp3";
                var mockStorageUrl = await DownloadAndUploadToStorageAsync(
                    response.ProviderFileUrl,
                    localFileName,
                    cancellationToken);

                audioRequest.ProviderAudioFileUrl = mockStorageUrl;
                audioRequest.Status = "AUDIO_PROVIDER_COMPLETED";
                audioRequest.CurrentStep = EventNames.AudioProviderCompleted;
                audioRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceAudioAsync(audioRequest, cancellationToken);

                await eventBus.PublishAsync(new AudioProviderCompletedEto
                {
                    CustomerContentId = audioRequest.CustomerContentId,
                    AnalysisContentId = audioRequest.AnalysisContentId,
                    ContentProcessType = @event.ContentProcessType,
                    CorrelationId = @event.CorrelationId,
                    VideoRequestId = audioRequest.VideoRequestId,
                    AudioRequestId = audioRequest.Id,
                    ProviderFileUrl = mockStorageUrl
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Audio provider track id is required.");

            audioRequest.AudioProviderTrackId = response.ProviderTrackId;
            audioRequest.Status = "AUDIO_PROVIDER_POLLING";
            audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(5);
            audioRequest.ProviderPollingCount = 0;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await eventBus.PublishAsync(new AudioProviderPollingStartedEto
            {
                CustomerContentId = audioRequest.CustomerContentId,
                AnalysisContentId = audioRequest.AnalysisContentId,
                ContentProcessType = @event.ContentProcessType,
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
            CustomerContentId = @event.CustomerContentId,
            AnalysisContentId = @event.AnalysisContentId,
            ContentProcessType = @event.ContentProcessType,
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
            audioRequest.Status = "DOWNLOADING";
            audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var localPath = await fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp3", audioRequest.ProviderFileName, cancellationToken);

            audioRequest.LocalAudioFilePath = localPath;
            audioRequest.Status = "DOWNLOADED";
            audioRequest.CurrentStep = EventNames.AudioFileDownloadCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await eventBus.PublishAsync(new AudioFileDownloadCompletedEto
            {
                CustomerContentId = audioRequest.CustomerContentId,
                AnalysisContentId = audioRequest.AnalysisContentId,
                ContentProcessType = @event.ContentProcessType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = audioRequest.VideoRequestId,
                AudioRequestId = audioRequest.Id,
                LocalFilePath = localPath
            }, cancellationToken);

            await eventBus.PublishAsync(new AudioFileUploadStartedEto
            {
                CustomerContentId = audioRequest.CustomerContentId,
                AnalysisContentId = audioRequest.AnalysisContentId,
                ContentProcessType = @event.ContentProcessType,
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
            audioRequest.Status = "UPLOADING";
            audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var storageUrl = await storageService.UploadAsync(@event.LocalFilePath, cancellationToken);

            audioRequest.AudioStorageUrl = storageUrl;
            audioRequest.Status = "UPLOADED";
            audioRequest.CurrentStep = EventNames.AudioFileUploadCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await eventBus.PublishAsync(new AudioFileUploadCompletedEto
            {
                CustomerContentId = audioRequest.CustomerContentId,
                AnalysisContentId = audioRequest.AnalysisContentId,
                ContentProcessType = @event.ContentProcessType,
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

        if (allAudios.Any(x => x.Status != "UPLOADED"))
            return;

        var orderedAudios = allAudios.OrderBy(x => x.SortOrder).ToList();

        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired &&
            orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.AudioStorageUrl)))
        {
            throw new InvalidOperationException("AudioStorageUrl is required for video provider.");
        }

        if (videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired &&
            orderedAudios.Any(x => string.IsNullOrWhiteSpace(x.LocalAudioFilePath)))
        {
            throw new InvalidOperationException("LocalAudioFilePath is required for video provider.");
        }

        var lockResult = await context.VideoRequests.UpdateOneAsync(
            x => x.Id == @event.VideoRequestId &&
                 x.Status != "VIDEO_PROVIDER_REQUEST_STARTING" &&
                 x.Status != "VIDEO_PROVIDER_REQUEST_STARTED" &&
                 x.Status != "VIDEO_PROVIDER_POLLING" &&
                 x.Status != "VIDEO_PROVIDER_COMPLETED" &&
                 x.Status != "VIDEO_DOWNLOADING" &&
                 x.Status != "VIDEO_UPLOADING" &&
                 x.Status != "COMPLETED" &&
                 x.Status != "FAILED",
            Builders<VideoRequest>.Update
                .Set(x => x.Status, "VIDEO_PROVIDER_REQUEST_STARTING")
                .Set(x => x.CurrentStep, EventNames.VideoProviderRequestStarted)
                .Set(x => x.LastError, null)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        if (lockResult.ModifiedCount == 0)
            return;

        videoRequest.Status = "VIDEO_PROVIDER_REQUEST_STARTING";

        await eventBus.PublishAsync(new VideoProviderRequestStartedEto
        {
            CustomerContentId = videoRequest.CustomerContentId,
            AnalysisContentId = videoRequest.AnalysisContentId,
            ContentProcessType = videoRequest.ContentProcessType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = videoRequest.Id,
            AudioUrls = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioUrlListRequired
                ? orderedAudios.Select(x => x.AudioStorageUrl!).ToList()
                : [],
            AudioFilePaths = videoProvider.Capabilities.AudioInputMode == VideoAudioInputMode.AudioFileRequired
                ? orderedAudios.Select(x => x.LocalAudioFilePath!).ToList()
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
            videoRequest.Status = "VIDEO_PROVIDER_REQUEST_STARTED";
            videoRequest.CurrentStep = EventNames.VideoProviderRequestStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var response = await provider.CreateAsync(new VideoCreateRequest { VideoInputJson = videoRequest.VideoInputJson, AudioUrls = @event.AudioUrls, AudioFilePaths = @event.AudioFilePaths }, cancellationToken);

            if (provider.Capabilities.ExecutionMode == ProviderExecutionMode.ImmediateResult)
            {
                if (string.IsNullOrWhiteSpace(response.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                videoRequest.ProviderFileName = response.FileName;

                // Download file from provider and upload to mock storage
                var localFileName = !string.IsNullOrWhiteSpace(response.FileName)
                    ? $"local_{response.FileName}"
                    : $"local_video_{videoRequest.Id:N}.mp4";
                var mockStorageUrl = await DownloadAndUploadToStorageAsync(
                    response.ProviderFileUrl,
                    localFileName,
                    cancellationToken);

                videoRequest.ProviderVideoFileUrl = mockStorageUrl;
                videoRequest.Status = "VIDEO_PROVIDER_COMPLETED";
                videoRequest.CurrentStep = EventNames.VideoProviderCompleted;
                videoRequest.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(videoRequest, cancellationToken);

                await eventBus.PublishAsync(new VideoProviderCompletedEto
                {
                    CustomerContentId = videoRequest.CustomerContentId,
                    AnalysisContentId = videoRequest.AnalysisContentId,
                    ContentProcessType = videoRequest.ContentProcessType,
                    CorrelationId = @event.CorrelationId,
                    VideoRequestId = videoRequest.Id,
                    ProviderFileUrl = mockStorageUrl
                }, cancellationToken);

                return;
            }

            if (string.IsNullOrWhiteSpace(response.ProviderTrackId))
                throw new InvalidOperationException("Video provider track id is required.");

            videoRequest.VideoProviderTrackId = response.ProviderTrackId;
            videoRequest.Status = "VIDEO_PROVIDER_POLLING";
            videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(5);
            videoRequest.ProviderPollingCount = 0;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoProviderPollingStartedEto
            {
                CustomerContentId = videoRequest.CustomerContentId,
                AnalysisContentId = videoRequest.AnalysisContentId,
                ContentProcessType = videoRequest.ContentProcessType,
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
            CustomerContentId = @event.CustomerContentId,
            AnalysisContentId = @event.AnalysisContentId,
            ContentProcessType = @event.ContentProcessType,
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
            videoRequest.Status = "VIDEO_DOWNLOADING";
            videoRequest.CurrentStep = EventNames.VideoFileDownloadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var localPath = await fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp4", videoRequest.ProviderFileName, cancellationToken);

            videoRequest.LocalVideoFilePath = localPath;
            videoRequest.Status = "VIDEO_DOWNLOADED";
            videoRequest.CurrentStep = EventNames.VideoFileDownloadCompleted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoFileUploadStartedEto
            {
                CustomerContentId = videoRequest.CustomerContentId,
                AnalysisContentId = videoRequest.AnalysisContentId,
                ContentProcessType = videoRequest.ContentProcessType,
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
            videoRequest.Status = "VIDEO_UPLOADING";
            videoRequest.CurrentStep = EventNames.VideoFileUploadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var storageUrl = await storageService.UploadAsync(@event.LocalFilePath, cancellationToken);

            videoRequest.FinalVideoStorageUrl = storageUrl;
            videoRequest.Status = "COMPLETED";
            videoRequest.CurrentStep = EventNames.VideoGenerationResultPublished;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await eventBus.PublishAsync(new VideoGenerationResultPublishedEto
            {
                CustomerContentId = videoRequest.CustomerContentId,
                AnalysisContentId = videoRequest.AnalysisContentId,
                ContentProcessType = videoRequest.ContentProcessType,
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
        audio.Status = "UPLOADED";
        audio.CurrentStep = EventNames.AudioFileUploadCompleted;
        audio.LastError = null;
        audio.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(audio, cancellationToken);

        await eventBus.PublishAsync(new AudioFileUploadCompletedEto
        {
            CustomerContentId = audio.CustomerContentId,
            AnalysisContentId = audio.AnalysisContentId,
            ContentProcessType = audio.ContentProcessType,
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

        if (audioRequest.Status is "AUDIO_PROVIDER_COMPLETED" or "UPLOADED" or "FAILED")
            return;

        audioRequest.AudioProviderTrackId = @event.ProviderTrackId;
        audioRequest.Status = "AUDIO_PROVIDER_POLLING";
        audioRequest.CurrentStep = EventNames.AudioProviderPollingStarted;

        if (audioRequest.NextProviderPollAtUtc is null)
            audioRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(5);

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

        if (videoRequest.Status is "VIDEO_PROVIDER_COMPLETED" or "COMPLETED" or "FAILED")
            return;

        videoRequest.VideoProviderTrackId = @event.ProviderTrackId;
        videoRequest.Status = "VIDEO_PROVIDER_POLLING";
        videoRequest.CurrentStep = EventNames.VideoProviderPollingStarted;

        if (videoRequest.NextProviderPollAtUtc is null)
            videoRequest.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(5);

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

    private Task<VideoRequest> GetVideoAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.VideoRequests.Find(x => x.Id == id).FirstAsync(cancellationToken);
    }

    private Task<AudioRequest> GetAudioAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.AudioRequests.Find(x => x.Id == id).FirstAsync(cancellationToken);
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

        if (request.RetryCount >= request.MaxRetryCount)
        {
            await FailAudioAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = "WAITING_RETRY";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            _retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CustomerContentId = request.CustomerContentId,
            AnalysisContentId = request.AnalysisContentId,
            ContentProcessType = request.ContentProcessType,
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
        request.Status = "FAILED";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CustomerContentId = request.CustomerContentId,
            AnalysisContentId = request.AnalysisContentId,
            ContentProcessType = request.ContentProcessType,
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

        if (request.RetryCount >= request.MaxRetryCount)
        {
            await FailVideoAsync(request, step, ex, false, cancellationToken);
            return;
        }

        request.Status = "WAITING_RETRY";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = DateTime.UtcNow.Add(
            _retryDelayCalculator.Calculate(request.RetryCount));

        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CustomerContentId = request.CustomerContentId,
            AnalysisContentId = request.AnalysisContentId,
            ContentProcessType = request.ContentProcessType,
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
        request.Status = "FAILED";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.NextRetryAtUtc = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(request, cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CustomerContentId = request.CustomerContentId,
            AnalysisContentId = request.AnalysisContentId,
            ContentProcessType = request.ContentProcessType,
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
            var storageUrl = "http://localhost:5048/storage/upload-binary";
            using (var content = new ByteArrayContent(fileContent))
            {
                var response = await httpClient.PostAsync(
                    $"{storageUrl}?fileName={fileName}",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonDoc = JsonDocument.Parse(responseJson);
                var remoteUrl = jsonDoc.RootElement.GetProperty("url").GetString();

                return remoteUrl ?? throw new InvalidOperationException("No URL in storage response");
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
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string ContentProcessType { get; set; } = ContentProcessTypes.CustomerContent;
    public string StorageUrl { get; set; } = default!;
}