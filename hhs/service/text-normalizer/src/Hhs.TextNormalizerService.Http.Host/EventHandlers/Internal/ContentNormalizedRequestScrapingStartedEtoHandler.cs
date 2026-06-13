using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class ContentNormalizedRequestScrapingStartedEtoHandler(
    IAppConsoleLogger logger,
    IContentNormalizedRequestAppService contentNormalizedRequestAppService
) : IIntegrationEventHandler<ContentNormalizedRequestScrapingStartedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IContentNormalizedRequestAppService _contentNormalizedRequestAppService = contentNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(contentNormalizedRequestAppService));

    public async Task HandleAsync(MessageEnvelope<ContentNormalizedRequestScrapingStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(ContentNormalizedRequestScrapingStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _contentNormalizedRequestAppService.SetParentIntegrationEvent(@event);
        await _contentNormalizedRequestAppService.ScrapingAsync(@event.Message.ContentNormalizedRequestId);
    }
}