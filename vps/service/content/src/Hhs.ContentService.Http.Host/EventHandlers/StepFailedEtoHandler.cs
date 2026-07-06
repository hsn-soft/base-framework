using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers;

public class StepFailedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<StepFailedEto>(inboxStore, logger, contentOperationService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<StepFailedEto> @event, CancellationToken cancellationToken)
        => await contentOperationService.HandleStepFailedAsync(@event.Message, cancellationToken);
}