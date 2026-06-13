using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class TestQueryRequestedEtoHandler(
    IAppConsoleLogger logger,
    ICustomerContentAppService customerContentAppService
) : IIntegrationEventHandler<TestQueryRequestedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICustomerContentAppService _customerContentAppService = customerContentAppService ?? throw new ArgumentNullException(nameof(customerContentAppService));

    public async Task HandleAsync(MessageEnvelope<TestQueryRequestedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(TestQueryRequestedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _customerContentAppService.SetParentIntegrationEvent(@event);
        await _customerContentAppService.TestQueryRequestedAsync(@event.Message);
    }
}