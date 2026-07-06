using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class NormalizerResultPublishedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<NormalizerResultPublishedEto>(inboxStore, logger, contentOperationService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<NormalizerResultPublishedEto> @event, CancellationToken cancellationToken)
        => await contentOperationService.HandleOutlineResultAsync(@event.Message, cancellationToken);
}