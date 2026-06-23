using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class AnalysisContentNormalizeRequestCreatedEtoHandler(
    IAppConsoleLogger logger,
    NormalizerOperationAppService normalizerOperationAppService
) : IIntegrationEventHandler<AnalysisContentNormalizeRequestCreatedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NormalizerOperationAppService _normalizerOperationAppService = normalizerOperationAppService ?? throw new ArgumentNullException(nameof(normalizerOperationAppService));

    public async Task HandleAsync(MessageEnvelope<AnalysisContentNormalizeRequestCreatedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AnalysisContentNormalizeRequestCreatedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _normalizerOperationAppService.SetParentIntegrationEvent(@event);
        await _normalizerOperationAppService.StartAnalysisContentNormalizeAsync(@event.Message);
    }
}