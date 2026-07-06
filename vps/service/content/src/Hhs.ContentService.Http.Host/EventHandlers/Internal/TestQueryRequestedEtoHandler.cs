using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class TestQueryRequestedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ICustomerContentAppService customerContentAppService
) : ApplicationEventHandlerBase<TestQueryRequestedEto>(inboxStore, logger, customerContentAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<TestQueryRequestedEto> @event, CancellationToken cancellationToken)
        => await customerContentAppService.TestQueryRequestedAsync(@event.Message);
}