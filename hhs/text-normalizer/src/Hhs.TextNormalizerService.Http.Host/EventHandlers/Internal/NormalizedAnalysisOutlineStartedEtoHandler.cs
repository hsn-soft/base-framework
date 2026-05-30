using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class NormalizedAnalysisOutlineStartedEtoHandler : IIntegrationEventHandler<NormalizedAnalysisOutlineStartedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly INormalizedAnalysisAppService _normalizedAnalysisAppService;

    public NormalizedAnalysisOutlineStartedEtoHandler(IAppConsoleLogger logger,
        INormalizedAnalysisAppService normalizedAnalysisAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizedAnalysisAppService = normalizedAnalysisAppService ?? throw new ArgumentNullException(nameof(normalizedAnalysisAppService));
    }

    public async Task HandleAsync(MessageEnvelope<NormalizedAnalysisOutlineStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(NormalizedAnalysisOutlineStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _normalizedAnalysisAppService.SetParentIntegrationEvent(@event);
        await _normalizedAnalysisAppService.OutlineAsync(@event.Message.NormalizedAnalysisId);
    }
}