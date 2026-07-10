using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers;

public class MilestoneFailedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<MilestoneFailedEto>(inboxStore, logger, contentOperationService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<MilestoneFailedEto> @event, CancellationToken cancellationToken)
        => await contentOperationService.HandleMilestoneFailedAsync(@event.Message, cancellationToken);
}