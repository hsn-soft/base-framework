using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class CustomerContentNormalizeRequestCreatedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<CustomerContentNormalizeRequestCreatedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationService _contentOperationService = contentOperationService ?? throw new ArgumentNullException(nameof(contentOperationService));

    protected override async Task ExecuteAsync(MessageEnvelope<CustomerContentNormalizeRequestCreatedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(CustomerContentNormalizeRequestCreatedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationService.HandleNormalizedRequestReferenceAsync(
            refContentType: ContentType.CustomerContent,
            refContentId: @event.Message.CustomerContentId,
            refNormalizeRequestId: @event.Message.CustomerContentNormalizeRequestId,
            normalizeStatus:@event.Message.NormalizeStatus,
            normalizeCurrentStep:@event.Message.NormalizeCurrentStep,
            correlationId: @event.CorrelationId
        );
    }
}