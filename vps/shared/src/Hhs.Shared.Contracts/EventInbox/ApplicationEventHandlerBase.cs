using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Hhs.Shared.Contracts.EventInbox;

public abstract class ApplicationEventHandlerBase<TEvent>(IEventInboxMessageManager inboxStore)
    : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEventMessage
{
    protected readonly IEventInboxMessageManager InboxStore = inboxStore;

    protected virtual IEventApplicationService AppService => null;

    public async Task HandleAsync(MessageEnvelope<TEvent> @event)
    {
        bool started = await InboxStore.StartAsync(@event, CancellationToken.None);

        if (!started)
            return;

        try
        {
            AppService?.SetParentIntegrationEvent(@event);
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
