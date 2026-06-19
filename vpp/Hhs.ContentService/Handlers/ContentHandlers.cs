using Hhs.ContentService.Infrastructure;
using Hhs.ContentService.Services;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;

namespace Hhs.ContentService.Handlers;

public abstract class ContentEtoHandlerBase<TEvent>(ContentInboxStore inboxStore) : IIntegrationEventHandler<TEvent>
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

public sealed class NormalizerResultPublishedEtoHandler(ContentInboxStore inboxStore, ContentOperationAppService appService) : ContentEtoHandlerBase<NormalizerResultPublishedEto>(inboxStore)
{
    protected override Task ExecuteAsync(NormalizerResultPublishedEto @event, CancellationToken cancellationToken)
        => appService.HandleNormalizerResultAsync(@event, cancellationToken);
}

public sealed class VideoGenerationResultPublishedEtoHandler(ContentInboxStore inboxStore, ContentOperationAppService appService) : ContentEtoHandlerBase<VideoGenerationResultPublishedEto>(inboxStore)
{
    protected override Task ExecuteAsync(VideoGenerationResultPublishedEto @event, CancellationToken cancellationToken)
        => appService.HandleVideoResultAsync(@event, cancellationToken);
}

public sealed class StepFailedEtoHandler(ContentInboxStore inboxStore, ContentOperationAppService appService) : ContentEtoHandlerBase<StepFailedEto>(inboxStore)
{
    protected override Task ExecuteAsync(StepFailedEto @event, CancellationToken cancellationToken)
        => appService.HandleStepFailedAsync(@event, cancellationToken);
}