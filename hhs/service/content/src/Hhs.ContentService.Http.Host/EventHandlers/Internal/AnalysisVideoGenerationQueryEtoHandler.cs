using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class AnalysisVideoGenerationQueryEtoHandler : IIntegrationEventHandler<AnalysisVideoGenerationQueryEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly IAnalysisContentAppService _analysisContentAppService;

    public AnalysisVideoGenerationQueryEtoHandler(IAppConsoleLogger logger,
        IAnalysisContentAppService analysisContentAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analysisContentAppService = analysisContentAppService ?? throw new ArgumentNullException(nameof(analysisContentAppService));
    }

    public async Task HandleAsync(MessageEnvelope<AnalysisVideoGenerationQueryEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AnalysisVideoGenerationQueryEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _analysisContentAppService.SetParentIntegrationEvent(@event);
        await _analysisContentAppService.AnalysisVideoGenerationQueryAsync(@event.Message);
    }
}