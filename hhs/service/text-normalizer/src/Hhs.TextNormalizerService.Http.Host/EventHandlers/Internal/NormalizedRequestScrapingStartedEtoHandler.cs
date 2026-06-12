using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class NormalizedRequestScrapingStartedEtoHandler : IIntegrationEventHandler<NormalizedRequestScrapingStartedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly INormalizedRequestAppService _normalizedRequestAppService;

    public NormalizedRequestScrapingStartedEtoHandler(IAppConsoleLogger logger,
        INormalizedRequestAppService normalizedRequestAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizedRequestAppService = normalizedRequestAppService ?? throw new ArgumentNullException(nameof(normalizedRequestAppService));
    }

    public async Task HandleAsync(MessageEnvelope<NormalizedRequestScrapingStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(NormalizedRequestScrapingStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _normalizedRequestAppService.SetParentIntegrationEvent(@event);
        await _normalizedRequestAppService.ScrapingAsync(@event.Message.NormalizedRequestId);
    }
}