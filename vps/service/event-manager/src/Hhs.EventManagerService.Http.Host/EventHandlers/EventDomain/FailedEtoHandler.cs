using Hhs.EventManagerService.Application.Contracts.EventDomain.Interfaces;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.EventManagerService.EventHandlers.EventDomain;

public class FailedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IFailedIntegrationEventAppService failedIntegrationEventAppService
) : ApplicationEventHandlerBase<FailedEto>(inboxStore, logger, failedIntegrationEventAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<FailedEto> @event, CancellationToken cancellationToken)
        => await failedIntegrationEventAppService.CreateAsync(@event.Message, @event.ParentMessageId);
}