using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.Shared.Contracts.Events.Content;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class ContentNormalizedRequestCreatedEtoHandler(
    IAppConsoleLogger logger,
    ICustomerContentAppService customerContentAppService
) : IIntegrationEventHandler<ContentNormalizedRequestCreatedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICustomerContentAppService _customerContentAppService = customerContentAppService ?? throw new ArgumentNullException(nameof(customerContentAppService));

    public async Task HandleAsync(MessageEnvelope<ContentNormalizedRequestCreatedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(ContentNormalizedRequestCreatedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _customerContentAppService.SetParentIntegrationEvent(@event);
        await _customerContentAppService.SetCustomerContentNormalizedReferenceAsync(@event.Message.CustomerContentId, @event.Message.ContentNormalizedRequestId);
    }
}