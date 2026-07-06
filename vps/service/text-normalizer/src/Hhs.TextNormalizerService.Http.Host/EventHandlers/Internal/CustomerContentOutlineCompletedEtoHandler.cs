using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class CustomerContentOutlineCompletedEtoHandler(
    IAppConsoleLogger logger,
    IEventInboxMessageManager inboxStore,
    NormalizerOperationAppService normalizerOperationAppService
) : ApplicationEventHandlerBase<CustomerContentOutlineCompletedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NormalizerOperationAppService _normalizerOperationAppService = normalizerOperationAppService ?? throw new ArgumentNullException(nameof(normalizerOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<CustomerContentOutlineCompletedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(CustomerContentOutlineCompletedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty);

        await _normalizerOperationAppService.CompleteCustomerContentOutlineAsync(@event.Message);
    }
}