using Hhs.EventManagerService.Application.Contracts.EventDomain.Interfaces;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.EventManagerService.EventHandlers.EventDomain;

public class FailedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IFailedIntegrationEventAppService failedIntegrationEventAppService
) : ApplicationEventHandlerBase<FailedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFailedIntegrationEventAppService _failedIntegrationEventAppService = failedIntegrationEventAppService ?? throw new ArgumentNullException(nameof(failedIntegrationEventAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<FailedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(FailedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _failedIntegrationEventAppService.SetParentIntegrationEvent(@event);
        await _failedIntegrationEventAppService.CreateAsync(@event.Message, @event.ParentMessageId);
    }
}