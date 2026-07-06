using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class CustomerContentScrapingCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<CustomerContentScrapingCompletedEto>(inboxStore, logger, contentOperationService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<CustomerContentScrapingCompletedEto> @event, CancellationToken cancellationToken)
        => await contentOperationService.HandleCustomerContentScrapeResultsAsync(@event.Message, @event.CorrelationId, cancellationToken);
}