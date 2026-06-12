using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.Shared.Contracts.Events.Content;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class AnalysisContentNormalizedResultEtoHandler : IIntegrationEventHandler<AnalysisContentNormalizedResultEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly IAnalysisContentAppService _analysisContentAppService;

    public AnalysisContentNormalizedResultEtoHandler(IAppConsoleLogger logger,
        IAnalysisContentAppService analysisContentAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analysisContentAppService = analysisContentAppService ?? throw new ArgumentNullException(nameof(analysisContentAppService));
    }

    public async Task HandleAsync(MessageEnvelope<AnalysisContentNormalizedResultEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AnalysisContentNormalizedResultEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _analysisContentAppService.SetParentIntegrationEvent(@event);
        await _analysisContentAppService.SetAnalysisContentNormalizedResultAsync(@event.Message.AnalysisContentId, @event.Message.NormalizedAnalysisId,
            @event.Message.IsNormalizedSuccess, @event.CorrelationId);
    }
}