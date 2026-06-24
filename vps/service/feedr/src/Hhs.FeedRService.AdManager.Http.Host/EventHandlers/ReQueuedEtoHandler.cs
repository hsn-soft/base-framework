using Hhs.FeedRService.Application.Contracts;
using Hhs.FeedRService.Application.Infrastructure;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.FeedRService.AdManager.EventHandlers;

public class ReQueuedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IEventManagerAppService eventManagerAppService
) : ApplicationEventHandlerBase<ReQueuedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IEventManagerAppService _eventManagerAppService = eventManagerAppService ?? throw new ArgumentNullException(nameof(eventManagerAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<ReQueuedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(ReQueuedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _eventManagerAppService.SetParentIntegrationEvent(@event);
        await _eventManagerAppService.EventReQueuedAsync(@event);
    }
}