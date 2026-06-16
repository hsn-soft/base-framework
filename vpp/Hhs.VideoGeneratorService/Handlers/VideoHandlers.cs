using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Infrastructure;
using Hhs.VideoGeneratorService.Services;

namespace Hhs.VideoGeneratorService.Handlers;

public abstract class VideoEventHandlerBase<TEvent>(VideoGeneratorInboxStore inboxStore) : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken)
    {
        bool started = await inboxStore.StartAsync(@event, cancellationToken);

        if (!started)
            return;

        try
        {
            await ExecuteAsync(@event, cancellationToken);
            await inboxStore.CompleteAsync(@event.EventId, cancellationToken);
        }
        catch (Exception ex)
        {
            await inboxStore.FailAsync(@event.EventId, ex, cancellationToken);
            throw;
        }
    }

    protected abstract Task ExecuteAsync(TEvent @event, CancellationToken cancellationToken);
}

public sealed class VideoGenerationApprovedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService)
    : VideoEventHandlerBase<VideoGenerationApprovedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoGenerationApprovedEvent @event, CancellationToken cancellationToken)
        => appService.CreateVideoRequestAsync(@event, cancellationToken);
}

public sealed class VideoRequestCreatedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoRequestCreatedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoRequestCreatedEvent @event, CancellationToken cancellationToken)
        => appService.StartVideoOperationAsync(@event, cancellationToken);
}

public sealed class VideoOperationStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoOperationStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoOperationStartedEvent @event, CancellationToken cancellationToken)
        => appService.HandleVideoOperationStartedAsync(@event, cancellationToken);
}

public sealed class AudioProviderRequestStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioProviderRequestStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AudioProviderRequestStartedEvent @event, CancellationToken cancellationToken)
        => appService.StartAudioProviderRequestAsync(@event, cancellationToken);
}

public sealed class AudioProviderCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioProviderCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AudioProviderCompletedEvent @event, CancellationToken cancellationToken)
        => appService.HandleAudioProviderCompletedAsync(@event, cancellationToken);
}

public sealed class AudioFileDownloadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioFileDownloadStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileDownloadStartedEvent @event, CancellationToken cancellationToken)
        => appService.DownloadAudioFileAsync(@event, cancellationToken);
}

public sealed class AudioFileDownloadCompletedEventHandler(VideoGeneratorInboxStore inboxStore) : VideoEventHandlerBase<AudioFileDownloadCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileDownloadCompletedEvent @event, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public sealed class AudioFileUploadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioFileUploadStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileUploadStartedEvent @event, CancellationToken cancellationToken)
        => appService.UploadAudioFileAsync(@event, cancellationToken);
}

public sealed class AudioFileUploadCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioFileUploadCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileUploadCompletedEvent @event, CancellationToken cancellationToken)
        => appService.HandleAudioUploadCompletedAsync(@event, cancellationToken);
}

public sealed class VideoProviderRequestStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoProviderRequestStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoProviderRequestStartedEvent @event, CancellationToken cancellationToken)
        => appService.StartVideoProviderRequestAsync(@event, cancellationToken);
}

public sealed class VideoProviderCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoProviderCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoProviderCompletedEvent @event, CancellationToken cancellationToken)
        => appService.HandleVideoProviderCompletedAsync(@event, cancellationToken);
}

public sealed class VideoFileDownloadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoFileDownloadStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoFileDownloadStartedEvent @event, CancellationToken cancellationToken)
        => appService.DownloadVideoFileAsync(@event, cancellationToken);
}

public sealed class VideoFileUploadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoFileUploadStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoFileUploadStartedEvent @event, CancellationToken cancellationToken)
        => appService.UploadVideoFileAsync(@event, cancellationToken);
}

public sealed class AudioProviderPollingStartedEventHandler(
    VideoGeneratorInboxStore inboxStore,
    VideoOperationAppService appService)
    : VideoEventHandlerBase<AudioProviderPollingStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(
        AudioProviderPollingStartedEvent @event,
        CancellationToken cancellationToken)
        => appService.ScheduleAudioProviderPollingAsync(@event, cancellationToken);
}

public sealed class VideoProviderPollingStartedEventHandler(
    VideoGeneratorInboxStore inboxStore,
    VideoOperationAppService appService)
    : VideoEventHandlerBase<VideoProviderPollingStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(
        VideoProviderPollingStartedEvent @event,
        CancellationToken cancellationToken)
        => appService.ScheduleVideoProviderPollingAsync(@event, cancellationToken);
}