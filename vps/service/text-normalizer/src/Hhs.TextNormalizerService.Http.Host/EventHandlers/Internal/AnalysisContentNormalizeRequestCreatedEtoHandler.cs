using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class AnalysisContentNormalizeRequestCreatedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    NormalizerOperationAppService normalizerOperationAppService
) : ApplicationEventHandlerBase<AnalysisContentNormalizeRequestCreatedEto>(inboxStore, logger, normalizerOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<AnalysisContentNormalizeRequestCreatedEto> @event, CancellationToken cancellationToken)
        => await normalizerOperationAppService.StartAnalysisContentNormalizeAsync(@event.Message, cancellationToken);
}