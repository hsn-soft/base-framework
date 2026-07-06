using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class AnalysisVideoGenerationQueryEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IAnalysisContentAppService analysisContentAppService
) : ApplicationEventHandlerBase<AnalysisVideoGenerationQueryEto>(inboxStore, logger, analysisContentAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<AnalysisVideoGenerationQueryEto> @event, CancellationToken cancellationToken)
        => await analysisContentAppService.AnalysisVideoGenerationQueryAsync(@event.Message);
}