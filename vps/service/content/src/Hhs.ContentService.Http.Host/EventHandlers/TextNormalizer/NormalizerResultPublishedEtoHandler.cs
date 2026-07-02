using Hhs.ContentService.Application.Infrastructure;
using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class NormalizerResultPublishedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<NormalizerResultPublishedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationService _contentOperationService = contentOperationService ?? throw new ArgumentNullException(nameof(contentOperationService));

    protected override async Task ExecuteAsync(MessageEnvelope<NormalizerResultPublishedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(NormalizerResultPublishedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationService.HandleOutlineResultAsync(@event.Message, cancellationToken);
    }
}