using Hhs.TextNormalizerService.Application.Infrastructure;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Hhs.TextNormalizerService.EventHandlers;

public abstract class ApplicationEventHandlerBase<TEvent>(ApplicationEventInboxMessageManager inboxStore)
    : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEventMessage
{
    protected readonly ApplicationEventInboxMessageManager InboxStore = inboxStore;

    public async Task HandleAsync(MessageEnvelope<TEvent> @event)
    {
        bool started = await InboxStore.StartAsync(@event, CancellationToken.None);

        if (!started)
            return;

        try
        {
            await ExecuteAsync(@event, CancellationToken.None);
            await InboxStore.CompleteAsync(@event.MessageId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await InboxStore.FailAsync(@event.MessageId, ex, CancellationToken.None);
            throw;
        }
    }

    protected abstract Task ExecuteAsync(MessageEnvelope<TEvent> @event, CancellationToken cancellationToken);
}
