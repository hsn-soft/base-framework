using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class ContentNormalizedRequestOutlineStartedEtoHandler(
    IAppConsoleLogger logger,
    IContentNormalizedRequestAppService contentNormalizedRequestAppService
) : IIntegrationEventHandler<ContentNormalizedRequestOutlineStartedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IContentNormalizedRequestAppService _contentNormalizedRequestAppService = contentNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(contentNormalizedRequestAppService));

    public async Task HandleAsync(MessageEnvelope<ContentNormalizedRequestOutlineStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(ContentNormalizedRequestOutlineStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _contentNormalizedRequestAppService.SetParentIntegrationEvent(@event);
        await _contentNormalizedRequestAppService.OutlineAsync(@event.Message.ContentNormalizedRequestId);
    }
}