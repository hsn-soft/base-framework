using Hhs.VideoGeneratorService.Application.Contracts;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers;

public class ReQueuedEtoHandler(
    IAppConsoleLogger logger,
    IEventManagerAppService eventManagerAppService
) : IIntegrationEventHandler<ReQueuedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IEventManagerAppService _eventManagerAppService = eventManagerAppService ?? throw new ArgumentNullException(nameof(eventManagerAppService));

    public async Task HandleAsync(MessageEnvelope<ReQueuedEto> @event)
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