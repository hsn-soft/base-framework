using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class AnalysisItemOutlineStartedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    NormalizerOperationAppService normalizerOperationAppService
) : ApplicationEventHandlerBase<AnalysisItemOutlineStartedEto>(inboxStore, logger, normalizerOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<AnalysisItemOutlineStartedEto> @event, CancellationToken cancellationToken)
        => await normalizerOperationAppService.StartAnalysisItemOutlineAsync(@event.Message, cancellationToken);
}