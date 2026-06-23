using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class AnalysisNormalizedRequestOutlineStartedEtoHandler(
    IAppConsoleLogger logger,
    IAnalysisNormalizedRequestAppService analysisNormalizedRequestAppService
) : IIntegrationEventHandler<AnalysisNormalizedRequestOutlineStartedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IAnalysisNormalizedRequestAppService _analysisNormalizedRequestAppService = analysisNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(analysisNormalizedRequestAppService));

    public async Task HandleAsync(MessageEnvelope<AnalysisNormalizedRequestOutlineStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AnalysisNormalizedRequestOutlineStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _analysisNormalizedRequestAppService.SetParentIntegrationEvent(@event);
        await _analysisNormalizedRequestAppService.OutlineAsync(@event.Message.AnalysisNormalizedRequestId);
    }
}