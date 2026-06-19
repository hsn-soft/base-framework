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

public sealed class VideoGenerationApprovedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoGenerationApprovedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoGenerationApprovedEto @event, CancellationToken cancellationToken)
        => appService.CreateVideoRequestAsync(@event, cancellationToken);
}

public sealed class VideoRequestCreatedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoRequestCreatedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoRequestCreatedEto @event, CancellationToken cancellationToken)
        => appService.StartVideoOperationAsync(@event, cancellationToken);
}

public sealed class VideoOperationStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoOperationStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoOperationStartedEto @event, CancellationToken cancellationToken)
        => appService.HandleVideoOperationStartedAsync(@event, cancellationToken);
}

public sealed class AudioProviderRequestStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioProviderRequestStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AudioProviderRequestStartedEto @event, CancellationToken cancellationToken)
        => appService.StartAudioProviderRequestAsync(@event, cancellationToken);
}

public sealed class AudioProviderCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioProviderCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AudioProviderCompletedEto @event, CancellationToken cancellationToken)
        => appService.HandleAudioProviderCompletedAsync(@event, cancellationToken);
}

public sealed class AudioFileDownloadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioFileDownloadStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileDownloadStartedEto @event, CancellationToken cancellationToken)
        => appService.DownloadAudioFileAsync(@event, cancellationToken);
}

public sealed class AudioFileDownloadCompletedEventHandler(VideoGeneratorInboxStore inboxStore) : VideoEventHandlerBase<AudioFileDownloadCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileDownloadCompletedEto @event, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public sealed class AudioFileUploadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioFileUploadStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileUploadStartedEto @event, CancellationToken cancellationToken)
        => appService.UploadAudioFileAsync(@event, cancellationToken);
}

public sealed class AudioFileUploadCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioFileUploadCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AudioFileUploadCompletedEto @event, CancellationToken cancellationToken)
        => appService.HandleAudioUploadCompletedAsync(@event, cancellationToken);
}

public sealed class VideoProviderRequestStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoProviderRequestStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoProviderRequestStartedEto @event, CancellationToken cancellationToken)
        => appService.StartVideoProviderRequestAsync(@event, cancellationToken);
}

public sealed class VideoProviderCompletedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoProviderCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoProviderCompletedEto @event, CancellationToken cancellationToken)
        => appService.HandleVideoProviderCompletedAsync(@event, cancellationToken);
}

public sealed class VideoFileDownloadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoFileDownloadStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoFileDownloadStartedEto @event, CancellationToken cancellationToken)
        => appService.DownloadVideoFileAsync(@event, cancellationToken);
}

public sealed class VideoFileUploadStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoFileUploadStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoFileUploadStartedEto @event, CancellationToken cancellationToken)
        => appService.UploadVideoFileAsync(@event, cancellationToken);
}

public sealed class AudioProviderPollingStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<AudioProviderPollingStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AudioProviderPollingStartedEto @event, CancellationToken cancellationToken)
        => appService.ScheduleAudioProviderPollingAsync(@event, cancellationToken);
}

public sealed class VideoProviderPollingStartedEventHandler(VideoGeneratorInboxStore inboxStore, VideoOperationAppService appService) : VideoEventHandlerBase<VideoProviderPollingStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoProviderPollingStartedEto @event, CancellationToken cancellationToken)
        => appService.ScheduleVideoProviderPollingAsync(@event, cancellationToken);
}