using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.EventInbox;

public abstract class ApplicationEventHandlerBase<TEvent>(
    [NotNull] IEventInboxMessageManager inboxStore,
    [NotNull] IAppConsoleLogger logger,
    [CanBeNull] IEventApplicationService appService = null
) : IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEventMessage
{
    private readonly IEventInboxMessageManager _inboxStore = inboxStore ?? throw new ArgumentNullException(nameof(inboxStore));
    protected readonly IAppConsoleLogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(MessageEnvelope<TEvent> @event)
    {
        bool started = await _inboxStore.StartAsync(@event, CancellationToken.None);

        if (!started)
            return;

        try
        {
            appService?.SetParentIntegrationEvent(@event);

            Logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
                nameof(@event.Message.GetType)[..^"Eto".Length],
                @event.CorrelationId ?? string.Empty,
                @event.MessageId.ToString(),
                @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

            await ExecuteAsync(@event, CancellationToken.None);

            await _inboxStore.CompleteAsync(@event.MessageId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await _inboxStore.FailAsync(@event.MessageId, ex, CancellationToken.None);
            throw;
        }
    }

    protected abstract Task ExecuteAsync(MessageEnvelope<TEvent> @event, CancellationToken cancellationToken);
}