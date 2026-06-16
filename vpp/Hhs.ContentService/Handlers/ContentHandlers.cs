using Hhs.ContentService.Infrastructure;
using Hhs.ContentService.Services;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;

namespace Hhs.ContentService.Handlers;

public abstract class ContentEventHandlerBase<TEvent>(ContentInboxStore inboxStore) : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken)
    {
        if (await inboxStore.IsProcessedAsync(@event.EventId, cancellationToken))
            return;

        await inboxStore.StartAsync(@event, cancellationToken);

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

public sealed class NormalizerResultPublishedEventHandler(ContentInboxStore inboxStore, ContentOperationAppService appService)
    : ContentEventHandlerBase<NormalizerResultPublishedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(NormalizerResultPublishedEvent @event, CancellationToken cancellationToken)
        => appService.HandleNormalizerResultAsync(@event, cancellationToken);
}

public sealed class VideoGenerationResultPublishedEventHandler(ContentInboxStore inboxStore, ContentOperationAppService appService)
    : ContentEventHandlerBase<VideoGenerationResultPublishedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(VideoGenerationResultPublishedEvent @event, CancellationToken cancellationToken)
        => appService.HandleVideoResultAsync(@event, cancellationToken);
}

public sealed class StepFailedEventHandler(
    ContentInboxStore inboxStore,
    ContentOperationAppService appService)
    : ContentEventHandlerBase<StepFailedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(
        StepFailedEvent @event,
        CancellationToken cancellationToken)
        => appService.HandleStepFailedAsync(@event, cancellationToken);
}