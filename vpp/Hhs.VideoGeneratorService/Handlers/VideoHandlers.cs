using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Infrastructure;
using Hhs.VideoGeneratorService.Services;

namespace Hhs.VideoGeneratorService.Handlers;

public abstract class VideoEventHandlerBase<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    private readonly VideoGeneratorInboxStore _inboxStore;

    protected VideoEventHandlerBase(VideoGeneratorInboxStore inboxStore)
    {
        _inboxStore = inboxStore;
    }

    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken)
    {
        if (await _inboxStore.ExistsAsync(@event.EventId, cancellationToken))
            return;

        await _inboxStore.SaveAsync(@event, cancellationToken);
        await ExecuteAsync(@event, cancellationToken);
    }

    protected abstract Task ExecuteAsync(TEvent @event, CancellationToken cancellationToken);
}

public sealed class VideoGenerationApprovedEventHandler
    : VideoEventHandlerBase<VideoGenerationApprovedEvent>
{
    private readonly VideoOperationAppService _appService;

    public VideoGenerationApprovedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoGenerationApprovedEvent @event, CancellationToken cancellationToken)
        => _appService.CreateVideoRequestAsync(@event, cancellationToken);
}

public sealed class VideoRequestCreatedEventHandler
    : VideoEventHandlerBase<VideoRequestCreatedEvent>
{
    private readonly VideoOperationAppService _appService;

    public VideoRequestCreatedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoRequestCreatedEvent @event, CancellationToken cancellationToken)
        => _appService.StartVideoOperationAsync(@event, cancellationToken);
}

public sealed class VideoOperationStartedEventHandler
    : VideoEventHandlerBase<VideoOperationStartedEvent>
{
    private readonly VideoOperationAppService _appService;

    public VideoOperationStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoOperationStartedEvent @event, CancellationToken cancellationToken)
        => _appService.HandleVideoOperationStartedAsync(@event, cancellationToken);
}

public sealed class AudioProviderRequestStartedEventHandler
    : VideoEventHandlerBase<AudioProviderRequestStartedEvent>
{
    private readonly VideoOperationAppService _appService;

    public AudioProviderRequestStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(AudioProviderRequestStartedEvent @event, CancellationToken cancellationToken)
        => _appService.StartAudioProviderRequestAsync(@event, cancellationToken);
}

public sealed class AudioProviderCompletedEventHandler
    : VideoEventHandlerBase<AudioProviderCompletedEvent>
{
    private readonly VideoOperationAppService _appService;

    public AudioProviderCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(AudioProviderCompletedEvent @event, CancellationToken cancellationToken)
        => _appService.HandleAudioProviderCompletedAsync(@event, cancellationToken);
}

public sealed class AudioFileDownloadStartedEventHandler
    : VideoEventHandlerBase<AudioFileDownloadStartedEvent>
{
    private readonly VideoOperationAppService _appService;

    public AudioFileDownloadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(AudioFileDownloadStartedEvent @event, CancellationToken cancellationToken)
        => _appService.DownloadAudioFileAsync(@event, cancellationToken);
}

public sealed class AudioFileUploadStartedEventHandler
    : VideoEventHandlerBase<AudioFileUploadStartedEvent>
{
    private readonly VideoOperationAppService _appService;

    public AudioFileUploadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(AudioFileUploadStartedEvent @event, CancellationToken cancellationToken)
        => _appService.UploadAudioFileAsync(@event, cancellationToken);
}

public sealed class AudioFileUploadCompletedEventHandler
    : VideoEventHandlerBase<AudioFileUploadCompletedEvent>
{
    private readonly VideoOperationAppService _appService;

    public AudioFileUploadCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(AudioFileUploadCompletedEvent @event, CancellationToken cancellationToken)
        => _appService.HandleAudioUploadCompletedAsync(@event, cancellationToken);
}

public sealed class VideoProviderRequestStartedEventHandler
    : VideoEventHandlerBase<VideoProviderRequestStartedEvent>
{
    private readonly VideoOperationAppService _appService;

    public VideoProviderRequestStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoProviderRequestStartedEvent @event, CancellationToken cancellationToken)
        => _appService.StartVideoProviderRequestAsync(@event, cancellationToken);
}

public sealed class VideoProviderCompletedEventHandler
    : VideoEventHandlerBase<VideoProviderCompletedEvent>
{
    private readonly VideoOperationAppService _appService;

    public VideoProviderCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoProviderCompletedEvent @event, CancellationToken cancellationToken)
        => _appService.HandleVideoProviderCompletedAsync(@event, cancellationToken);
}

public sealed class VideoFileDownloadStartedEventHandler
    : VideoEventHandlerBase<VideoFileDownloadStartedEvent>
{
    private readonly VideoOperationAppService _appService;

    public VideoFileDownloadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoFileDownloadStartedEvent @event, CancellationToken cancellationToken)
        => _appService.DownloadVideoFileAsync(@event, cancellationToken);
}

public sealed class VideoFileUploadStartedEventHandler
    : VideoEventHandlerBase<VideoFileUploadStartedEvent>
{
    private readonly VideoOperationAppService _appService;

    public VideoFileUploadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : base(inboxStore)
    {
        _appService = appService;
    }

    protected override Task ExecuteAsync(VideoFileUploadStartedEvent @event, CancellationToken cancellationToken)
        => _appService.UploadVideoFileAsync(@event, cancellationToken);
}