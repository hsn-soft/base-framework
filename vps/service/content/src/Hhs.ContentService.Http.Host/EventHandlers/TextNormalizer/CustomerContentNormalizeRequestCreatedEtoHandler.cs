using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class CustomerContentNormalizeRequestCreatedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<CustomerContentNormalizeRequestCreatedEto>(inboxStore, logger, contentOperationService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<CustomerContentNormalizeRequestCreatedEto> @event, CancellationToken cancellationToken)
        => await contentOperationService.HandleNormalizedRequestReferenceAsync(
            refContentType: ContentType.CustomerContent,
            refContentId: @event.Message.CustomerContentId,
            refNormalizeRequestId: @event.Message.CustomerContentNormalizeRequestId,
            normalizeStatus: @event.Message.NormalizeStatus,
            normalizeCurrentMilestone: @event.Message.NormalizeCurrentMilestone,
            correlationId: @event.CorrelationId
        );
}