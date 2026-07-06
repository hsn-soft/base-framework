using Hhs.FeedRService.Application.Contracts.DashboardDomain;
using Hhs.FeedRService.Application.Contracts.Events.Reporting;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.FeedRService.AdManager.EventHandlers.Internal;

public class DerivePendingReportsRequestedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IReportPersistenceService reportPersistenceService
) : ApplicationEventHandlerBase<DerivePendingReportsRequestedEto>(inboxStore, logger, reportPersistenceService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<DerivePendingReportsRequestedEto> @event, CancellationToken cancellationToken)
        => await reportPersistenceService.DerivePendingReportsAsync(@event.Message.MaxCount, cancellationToken);
}