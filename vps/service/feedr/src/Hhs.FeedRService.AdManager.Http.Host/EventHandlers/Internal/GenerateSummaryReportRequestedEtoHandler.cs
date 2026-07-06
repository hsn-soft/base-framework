using Hhs.FeedRService.Application.Contracts.Events.Reporting;
using Hhs.FeedRService.Application.Contracts.JobDomain;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.FeedRService.AdManager.EventHandlers.Internal;

public class GenerateSummaryReportRequestedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IGoogleReportService reportService
) : ApplicationEventHandlerBase<GenerateSummaryReportRequestedEto>(inboxStore, logger, reportService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<GenerateSummaryReportRequestedEto> @event, CancellationToken cancellationToken)
        => await reportService.GenerateSummaryReportAsync(@event.Message.AppClientId, @event.Message.DateRange);
}