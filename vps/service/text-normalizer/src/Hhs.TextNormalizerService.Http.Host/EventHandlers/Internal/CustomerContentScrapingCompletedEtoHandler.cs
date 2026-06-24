using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Infrastructure;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class CustomerContentScrapingCompletedEtoHandler(
    IAppConsoleLogger logger,
    NormalizerInboxStoreService inboxStore,
    NormalizerOperationAppService normalizerOperationAppService
) : NormalizerEventHandlerBase<CustomerContentScrapingCompletedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NormalizerOperationAppService _normalizerOperationAppService = normalizerOperationAppService ?? throw new ArgumentNullException(nameof(normalizerOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<CustomerContentScrapingCompletedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(CustomerContentScrapingCompletedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty);

        await _normalizerOperationAppService.CompleteCustomerContentScrapingAsync(@event.Message);
    }
}