using System.Text.Json;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class VideoOperationAppService
{
    private readonly VideoMongoContext _context;
    private readonly IAudioProvider _audioProvider;
    private readonly IVideoProvider _videoProvider;
    private readonly IFileDownloader _fileDownloader;
    private readonly IStorageService _storageService;
    private readonly IEventBus _eventBus;

    public VideoOperationAppService(
        VideoMongoContext context,
        IAudioProvider audioProvider,
        IVideoProvider videoProvider,
        IFileDownloader fileDownloader,
        IStorageService storageService,
        IEventBus eventBus)
    {
        _context = context;
        _audioProvider = audioProvider;
        _videoProvider = videoProvider;
        _fileDownloader = fileDownloader;
        _storageService = storageService;
        _eventBus = eventBus;
    }

    public async Task CreateVideoRequestAsync(VideoGenerationApprovedEvent @event, CancellationToken cancellationToken)
    {
        var videoRequestId = Guid.NewGuid();
        var isAnalysis = @event.AnalysisContentId.HasValue;

        var videoRequest = new VideoRequest
        {
            Id = videoRequestId,
            CustomerContentId = @event.CustomerContentId,
            AnalysisContentId = @event.AnalysisContentId,
            ContentProcessType = @event.ContentProcessType,
            Status = "CREATED",
            CurrentStep = EventNames.VideoRequestCreated,
            ExternalAudioRequired = true,
            VideoInputJson = @event.VideoInputJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _context.VideoRequests.InsertOneAsync(videoRequest, cancellationToken: cancellationToken);

        await _eventBus.PublishAsync(new VideoRequestCreatedEvent
        {
            CustomerContentId = @event.CustomerContentId,
            AnalysisContentId = @event.AnalysisContentId,
            ContentProcessType = @event.ContentProcessType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = videoRequestId,
            IsAnalysis = isAnalysis,
            ExternalAudioRequired = true
        }, cancellationToken);
    }

    public async Task StartVideoOperationAsync(VideoRequestCreatedEvent @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        videoRequest.Status = "STARTED";
        videoRequest.CurrentStep = EventNames.VideoOperationStarted;
        videoRequest.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(videoRequest, cancellationToken);

        await _eventBus.PublishAsync(new VideoOperationStartedEvent
        {
            CustomerContentId = videoRequest.CustomerContentId,
            AnalysisContentId = videoRequest.AnalysisContentId,
            ContentProcessType = videoRequest.ContentProcessType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = videoRequest.Id,
            ExternalAudioRequired = videoRequest.ExternalAudioRequired
        }, cancellationToken);
    }

    public async Task HandleVideoOperationStartedAsync(VideoOperationStartedEvent @event, CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        if (!videoRequest.ExternalAudioRequired)
        {
            await _eventBus.PublishAsync(new VideoProviderRequestStartedEvent
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
            var audioRequestId = Guid.NewGuid();

            var audioRequest = new AudioRequest
            {
                Id = audioRequestId,
                VideoRequestId = videoRequest.Id,
                CustomerContentId = item.CustomerContentId ?? videoRequest.CustomerContentId,
                AnalysisContentId = videoRequest.AnalysisContentId,
                SortOrder = item.SortOrder,
                InputText = item.Text,
                Status = "CREATED",
                CurrentStep = "AUDIO_REQUEST_CREATED",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _context.AudioRequests.InsertOneAsync(audioRequest, cancellationToken: cancellationToken);

            await _eventBus.PublishAsync(new AudioProviderRequestStartedEvent
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
        AudioProviderRequestStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = "PROVIDER_REQUEST_STARTED";
            audioRequest.CurrentStep = EventNames.AudioProviderRequestStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var response = await _audioProvider.CreateAudioAsync(@event.InputText, cancellationToken);

            audioRequest.AudioProviderRequestId = response.ProviderRequestId;
            audioRequest.ProviderAudioFileUrl = response.ProviderFileUrl;
            audioRequest.Status = "PROVIDER_COMPLETED";
            audioRequest.CurrentStep = EventNames.AudioProviderCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await _eventBus.PublishAsync(new AudioProviderCompletedEvent
            {
                CustomerContentId = audioRequest.CustomerContentId,
                AnalysisContentId = audioRequest.AnalysisContentId,
                ContentProcessType = @event.ContentProcessType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = audioRequest.VideoRequestId,
                AudioRequestId = audioRequest.Id,
                ProviderFileUrl = response.ProviderFileUrl
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await FailAudioAsync(audioRequest, EventNames.AudioProviderRequestStarted, ex, cancellationToken);
            throw;
        }
    }

    public async Task HandleAudioProviderCompletedAsync(
        AudioProviderCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(new AudioFileDownloadStartedEvent
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
        AudioFileDownloadStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = "DOWNLOADING";
            audioRequest.CurrentStep = EventNames.AudioFileDownloadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var localPath = await _fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp3", cancellationToken);

            audioRequest.LocalAudioFilePath = localPath;
            audioRequest.Status = "DOWNLOADED";
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await _eventBus.PublishAsync(new AudioFileUploadStartedEvent
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
            await FailAudioAsync(audioRequest, EventNames.AudioFileDownloadStarted, ex, cancellationToken);
            throw;
        }
    }

    public async Task UploadAudioFileAsync(
        AudioFileUploadStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var audioRequest = await GetAudioAsync(@event.AudioRequestId, cancellationToken);

        try
        {
            audioRequest.Status = "UPLOADING";
            audioRequest.CurrentStep = EventNames.AudioFileUploadStarted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            var storageUrl = await _storageService.UploadAsync(@event.LocalFilePath, cancellationToken);

            audioRequest.AudioStorageUrl = storageUrl;
            audioRequest.Status = "UPLOADED";
            audioRequest.CurrentStep = EventNames.AudioFileUploadCompleted;
            audioRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAudioAsync(audioRequest, cancellationToken);

            await _eventBus.PublishAsync(new AudioFileUploadCompletedEvent
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
            await FailAudioAsync(audioRequest, EventNames.AudioFileUploadStarted, ex, cancellationToken);
            throw;
        }
    }

    public async Task HandleAudioUploadCompletedAsync(
        AudioFileUploadCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        var allAudios = await _context.AudioRequests
            .Find(x => x.VideoRequestId == @event.VideoRequestId)
            .ToListAsync(cancellationToken);

        if (allAudios.Any(x => x.Status != "UPLOADED"))
        {
            return;
        }

        var audioUrls = allAudios
            .OrderBy(x => x.SortOrder)
            .Select(x => x.AudioStorageUrl!)
            .ToList();

        await _eventBus.PublishAsync(new VideoProviderRequestStartedEvent
        {
            CustomerContentId = @event.CustomerContentId,
            AnalysisContentId = @event.AnalysisContentId,
            ContentProcessType = @event.ContentProcessType,
            CorrelationId = @event.CorrelationId,
            VideoRequestId = @event.VideoRequestId,
            AudioUrls = audioUrls
        }, cancellationToken);
    }

    public async Task StartVideoProviderRequestAsync(
        VideoProviderRequestStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = "VIDEO_PROVIDER_REQUEST_STARTED";
            videoRequest.CurrentStep = EventNames.VideoProviderRequestStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var response = await _videoProvider.CreateVideoAsync(
                videoRequest.VideoInputJson,
                @event.AudioUrls,
                cancellationToken);

            videoRequest.VideoProviderRequestId = response.ProviderRequestId;
            videoRequest.ProviderVideoFileUrl = response.ProviderFileUrl;
            videoRequest.Status = "VIDEO_PROVIDER_COMPLETED";
            videoRequest.CurrentStep = EventNames.VideoProviderCompleted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await _eventBus.PublishAsync(new VideoProviderCompletedEvent
            {
                CustomerContentId = videoRequest.CustomerContentId,
                AnalysisContentId = videoRequest.AnalysisContentId,
                ContentProcessType = videoRequest.ContentProcessType,
                CorrelationId = @event.CorrelationId,
                VideoRequestId = videoRequest.Id,
                ProviderFileUrl = response.ProviderFileUrl
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await FailVideoAsync(videoRequest, EventNames.VideoProviderRequestStarted, ex, cancellationToken);
            throw;
        }
    }

    public async Task HandleVideoProviderCompletedAsync(
        VideoProviderCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(new VideoFileDownloadStartedEvent
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
        VideoFileDownloadStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = "VIDEO_DOWNLOADING";
            videoRequest.CurrentStep = EventNames.VideoFileDownloadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var localPath = await _fileDownloader.DownloadAsync(@event.ProviderFileUrl, "mp4", cancellationToken);

            videoRequest.LocalVideoFilePath = localPath;
            videoRequest.Status = "VIDEO_DOWNLOADED";
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await _eventBus.PublishAsync(new VideoFileUploadStartedEvent
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
            await FailVideoAsync(videoRequest, EventNames.VideoFileDownloadStarted, ex, cancellationToken);
            throw;
        }
    }

    public async Task UploadVideoFileAsync(
        VideoFileUploadStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var videoRequest = await GetVideoAsync(@event.VideoRequestId, cancellationToken);

        try
        {
            videoRequest.Status = "VIDEO_UPLOADING";
            videoRequest.CurrentStep = EventNames.VideoFileUploadStarted;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            var storageUrl = await _storageService.UploadAsync(@event.LocalFilePath, cancellationToken);

            videoRequest.FinalVideoStorageUrl = storageUrl;
            videoRequest.Status = "COMPLETED";
            videoRequest.CurrentStep = EventNames.VideoGenerationResultPublished;
            videoRequest.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceVideoAsync(videoRequest, cancellationToken);

            await _eventBus.PublishAsync(new VideoGenerationResultPublishedEvent
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
            await FailVideoAsync(videoRequest, EventNames.VideoFileUploadStarted, ex, cancellationToken);
            throw;
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

        await _eventBus.PublishAsync(new AudioFileUploadCompletedEvent
        {
            CustomerContentId = audio.CustomerContentId,
            AnalysisContentId = audio.AnalysisContentId,
            ContentProcessType = input.ContentProcessType,
            CorrelationId = input.CorrelationId,
            VideoRequestId = audio.VideoRequestId,
            AudioRequestId = audio.Id,
            StorageUrl = input.StorageUrl,
            IsManual = true
        }, cancellationToken);
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
        return _context.VideoRequests.Find(x => x.Id == id).FirstAsync(cancellationToken);
    }

    private Task<AudioRequest> GetAudioAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.AudioRequests.Find(x => x.Id == id).FirstAsync(cancellationToken);
    }

    private Task ReplaceVideoAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        return _context.VideoRequests.ReplaceOneAsync(x => x.Id == request.Id, request, cancellationToken: cancellationToken);
    }

    private Task ReplaceAudioAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        return _context.AudioRequests.ReplaceOneAsync(x => x.Id == request.Id, request, cancellationToken: cancellationToken);
    }

    private async Task FailAudioAsync(AudioRequest request, string step, Exception ex, CancellationToken cancellationToken)
    {
        request.Status = "FAILED";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.RetryCount++;
        request.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAudioAsync(request, cancellationToken);

        await _eventBus.PublishAsync(new StepFailedEvent
        {
            CustomerContentId = request.CustomerContentId,
            AnalysisContentId = request.AnalysisContentId,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }

    private async Task FailVideoAsync(VideoRequest request, string step, Exception ex, CancellationToken cancellationToken)
    {
        request.Status = "FAILED";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.RetryCount++;
        request.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceVideoAsync(request, cancellationToken);

        await _eventBus.PublishAsync(new StepFailedEvent
        {
            CustomerContentId = request.CustomerContentId,
            AnalysisContentId = request.AnalysisContentId,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
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