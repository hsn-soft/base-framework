using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class AnalysisContentNormalizeRequestCreatedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<AnalysisContentNormalizeRequestCreatedEto>(inboxStore, logger, contentOperationService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<AnalysisContentNormalizeRequestCreatedEto> @event, CancellationToken cancellationToken)
        => await contentOperationService.HandleNormalizedRequestReferenceAsync(
            refContentType: ContentType.AnalysisContent,
            refContentId: @event.Message.AnalysisContentId,
            refNormalizeRequestId: @event.Message.AnalysisContentNormalizeRequestId,
            normalizeStatus: @event.Message.NormalizeStatus,
            normalizeCurrentStep: @event.Message.NormalizeCurrentStep
        );
}