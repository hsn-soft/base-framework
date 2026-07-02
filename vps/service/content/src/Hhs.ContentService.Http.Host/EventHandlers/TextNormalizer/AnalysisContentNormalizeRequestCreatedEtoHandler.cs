using Hhs.ContentService.Application.Infrastructure;
using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class AnalysisContentNormalizeRequestCreatedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<AnalysisContentNormalizeRequestCreatedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationService _contentOperationService = contentOperationService ?? throw new ArgumentNullException(nameof(contentOperationService));

    protected override async Task ExecuteAsync(MessageEnvelope<AnalysisContentNormalizeRequestCreatedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(AnalysisContentNormalizeRequestCreatedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationService.HandleNormalizedRequestReferenceAsync(
            refContentType: ContentType.AnalysisContent,
            refContentId: @event.Message.AnalysisContentId,
            refNormalizeRequestId: @event.Message.AnalysisContentNormalizeRequestId,
            normalizeStatus:@event.Message.NormalizeStatus,
            normalizeCurrentStep:@event.Message.NormalizeCurrentStep
        );
    }
}