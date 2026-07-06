using Hhs.FeedRService.Application.Contracts.Events;
using Hhs.FeedRService.Application.Contracts.JobDomain;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.FeedRService.AdManager.EventHandlers.Internal;

public class TestQueryCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IGoogleReportService reportService
) : ApplicationEventHandlerBase<TestQueryCompletedEto>(inboxStore, logger, reportService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<TestQueryCompletedEto> @event, CancellationToken cancellationToken)
        => await reportService.RetrieveInventoriesAsync(@event.Message.AppClientId);
}