using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class AnalysisContentNormalizedStartedEtoHandler : IIntegrationEventHandler<AnalysisContentNormalizedStartedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly INormalizedAnalysisAppService _normalizedAnalysisAppService;

    public AnalysisContentNormalizedStartedEtoHandler(IAppConsoleLogger logger,
        INormalizedAnalysisAppService normalizedAnalysisAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizedAnalysisAppService = normalizedAnalysisAppService ?? throw new ArgumentNullException(nameof(normalizedAnalysisAppService));
    }

    public async Task HandleAsync(MessageEnvelope<AnalysisContentNormalizedStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AnalysisContentNormalizedStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _normalizedAnalysisAppService.SetParentIntegrationEvent(@event);
        await _normalizedAnalysisAppService.CreateAsync(@event.Message, @event.CorrelationId);
    }
}