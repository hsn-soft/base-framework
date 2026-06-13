using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class AnalysisContentNormalizedStartedEtoHandler(
    IAppConsoleLogger logger,
    IAnalysisNormalizedRequestAppService analysisNormalizedRequestAppService
) : IIntegrationEventHandler<AnalysisContentNormalizedStartedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IAnalysisNormalizedRequestAppService _analysisNormalizedRequestAppService = analysisNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(analysisNormalizedRequestAppService));

    public async Task HandleAsync(MessageEnvelope<AnalysisContentNormalizedStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AnalysisContentNormalizedStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _analysisNormalizedRequestAppService.SetParentIntegrationEvent(@event);
        await _analysisNormalizedRequestAppService.CreateAsync(@event.Message, @event.CorrelationId);
    }
}