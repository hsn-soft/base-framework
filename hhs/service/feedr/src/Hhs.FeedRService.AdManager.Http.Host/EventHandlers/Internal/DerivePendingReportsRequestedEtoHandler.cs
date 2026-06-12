using Hhs.FeedRService.Application.Contracts.DashboardDomain;
using Hhs.FeedRService.Application.Contracts.Events.Reporting;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.FeedRService.AdManager.EventHandlers.Internal;

public class DerivePendingReportsRequestedEtoHandler(
    IAppConsoleLogger logger,
    IReportPersistenceService reportPersistenceService,
    IRequestLimitStore limitStore) : IIntegrationEventHandler<DerivePendingReportsRequestedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IReportPersistenceService _reportPersistenceService = reportPersistenceService ?? throw new ArgumentNullException(nameof(reportPersistenceService));
    private readonly IRequestLimitStore _limitStore = limitStore ?? throw new ArgumentNullException(nameof(limitStore));
    private const string EndpointKey = "feedr-admanager:jobs:derive-pending-reports";

    public async Task HandleAsync(MessageEnvelope<DerivePendingReportsRequestedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(DerivePendingReportsRequestedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        await _reportPersistenceService.DerivePendingReportsAsync(@event.Message.MaxCount);
    }
}