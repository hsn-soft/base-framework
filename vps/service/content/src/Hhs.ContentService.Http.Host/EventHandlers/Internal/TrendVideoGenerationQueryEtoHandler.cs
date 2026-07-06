using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class TrendVideoGenerationQueryEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ICustomerContentAppService customerContentAppService
) : ApplicationEventHandlerBase<TrendVideoGenerationQueryEto>(inboxStore, logger, customerContentAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<TrendVideoGenerationQueryEto> @event, CancellationToken cancellationToken)
        => await customerContentAppService.TrendVideoGenerationQueryAsync(@event.Message);
}