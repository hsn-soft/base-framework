using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Infrastructure;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class AnalysisContentCreatedEtoHandler(
    IAppConsoleLogger logger,
    NormalizerInboxStore inboxStore,
    NormalizerOperationAppService normalizerOperationAppService
) : NormalizerEventHandlerBase<AnalysisContentCreatedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NormalizerOperationAppService _normalizerOperationAppService = normalizerOperationAppService ?? throw new ArgumentNullException(nameof(normalizerOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<AnalysisContentCreatedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(AnalysisContentCreatedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty);

        await _normalizerOperationAppService.CreateAnalysisContentNormalizeRequestAsync(@event.Message, @event.MessageId, @event.CorrelationId);
    }
}