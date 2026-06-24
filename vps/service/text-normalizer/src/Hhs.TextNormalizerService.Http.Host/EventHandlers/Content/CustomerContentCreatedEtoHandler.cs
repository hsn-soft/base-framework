using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Infrastructure;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class CustomerContentCreatedEtoHandler(
    IAppConsoleLogger logger,
    ApplicationEventInboxMessageManager inboxStore,
    NormalizerOperationAppService normalizerOperationAppService
) : NormalizerEventHandlerBase<CustomerContentCreatedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NormalizerOperationAppService _normalizerOperationAppService = normalizerOperationAppService ?? throw new ArgumentNullException(nameof(normalizerOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<CustomerContentCreatedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(CustomerContentCreatedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty);

        await _normalizerOperationAppService.CreateCustomerContentNormalizeRequestAsync(@event.Message, @event.MessageId, @event.CorrelationId);
    }
}