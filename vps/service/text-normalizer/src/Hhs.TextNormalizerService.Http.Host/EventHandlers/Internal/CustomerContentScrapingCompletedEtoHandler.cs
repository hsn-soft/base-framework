using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class CustomerContentScrapingCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    NormalizerOperationAppService normalizerOperationAppService
) : ApplicationEventHandlerBase<CustomerContentScrapingCompletedEto>(inboxStore, logger, normalizerOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<CustomerContentScrapingCompletedEto> @event, CancellationToken cancellationToken)
        => await normalizerOperationAppService.CompleteCustomerContentScrapingAsync(@event.Message, cancellationToken);
}