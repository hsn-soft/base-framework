using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class CustomerContentNormalizedStartedEtoHandler(
    IAppConsoleLogger logger,
    IContentNormalizedRequestAppService contentNormalizedRequestAppService
) : IIntegrationEventHandler<CustomerContentNormalizedStartedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IContentNormalizedRequestAppService _contentNormalizedRequestAppService = contentNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(contentNormalizedRequestAppService));

    public async Task HandleAsync(MessageEnvelope<CustomerContentNormalizedStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(CustomerContentNormalizedStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _contentNormalizedRequestAppService.SetParentIntegrationEvent(@event);
        await _contentNormalizedRequestAppService.CreateAsync(@event.Message, @event.CorrelationId);
    }
}