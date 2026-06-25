using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Infrastructure;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class VideoGenerationApprovedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    NormalizerOperationAppService normalizerOperationAppService
) : ApplicationEventHandlerBase<VideoGenerationApprovedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NormalizerOperationAppService _normalizerOperationAppService = normalizerOperationAppService ?? throw new ArgumentNullException(nameof(normalizerOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<VideoGenerationApprovedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(VideoGenerationApprovedEto)[..^"Eto".Length],
            @event.MessageId);

        await _normalizerOperationAppService.ForwardVideoGenerationDataAsync(@event.Message, cancellationToken);
    }
}
