using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class OutlineProviderCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    NormalizerOperationAppService normalizerOperationAppService
) : ApplicationEventHandlerBase<OutlineProviderCompletedEto>(inboxStore, logger, normalizerOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<OutlineProviderCompletedEto> @event, CancellationToken cancellationToken)
        => await normalizerOperationAppService.CompleteOutlineProviderAsync(@event.Message, cancellationToken);
}