using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.Shared.Contracts.Events.Content;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class AnalysisNormalizedRequestCreatedEtoHandler(
    IAppConsoleLogger logger,
    IAnalysisContentAppService analysisContentAppService
    ) : IIntegrationEventHandler<AnalysisNormalizedRequestCreatedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IAnalysisContentAppService _analysisContentAppService = analysisContentAppService ?? throw new ArgumentNullException(nameof(analysisContentAppService));

    public async Task HandleAsync(MessageEnvelope<AnalysisNormalizedRequestCreatedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AnalysisNormalizedRequestCreatedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _analysisContentAppService.SetParentIntegrationEvent(@event);
        await _analysisContentAppService.SetAnalysisContentNormalizedReferenceAsync(@event.Message.AnalysisContentId, @event.Message.AnalysisNormalizedRequestId);
    }
}