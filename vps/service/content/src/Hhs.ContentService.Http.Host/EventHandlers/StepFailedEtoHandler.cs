using Hhs.ContentService.Application.Infrastructure;
using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers;

public class StepFailedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationAppService contentOperationAppService
) : ContentEventHandlerBase<StepFailedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationAppService _contentOperationAppService = contentOperationAppService ?? throw new ArgumentNullException(nameof(contentOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<StepFailedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(StepFailedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationAppService.HandleStepFailedAsync(@event.Message, cancellationToken);
    }
}