using Hhs.EventManagerService.Application.Contracts.EventDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.EventManagerService.EventHandlers.EventDomain;

public class FailedEtoHandler : IIntegrationEventHandler<FailedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly IFailedIntegrationEventAppService _failedIntegrationEventAppService;

    public FailedEtoHandler(IAppConsoleLogger logger,
        IFailedIntegrationEventAppService failedIntegrationEventAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _failedIntegrationEventAppService = failedIntegrationEventAppService ?? throw new ArgumentNullException(nameof(failedIntegrationEventAppService));
    }

    public async Task HandleAsync(MessageEnvelope<FailedEto> @event)
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