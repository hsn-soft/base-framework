using Hhs.FeedRService.Application.Contracts.Events.Reporting;
using Hhs.FeedRService.Application.Contracts.JobDomain;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.FeedRService.AdManager.EventHandlers.Internal;

public class GenerateSummaryReportRequestedEtoHandler(
    IAppConsoleLogger logger,
    IGoogleReportService reportService,
    IRequestLimitStore limitStore) : IIntegrationEventHandler<GenerateSummaryReportRequestedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IGoogleReportService _googleReportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
    private readonly IRequestLimitStore _limitStore = limitStore ?? throw new ArgumentNullException(nameof(limitStore));
    private const string EndpointKey = "feedr-admanager:jobs:generate-summary-report";

    public async Task HandleAsync(MessageEnvelope<GenerateSummaryReportRequestedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(GenerateSummaryReportRequestedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        await _googleReportService.GenerateSummaryReportAsync(@event.Message.AppClientId,@event.Message.DateRange);
    }
}