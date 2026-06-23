namespace Hhs.ContentService.EventHandlers.Internal;

// public class DashboardResponseStatisticQueryEtoHandler(
//     IAppConsoleLogger logger,
//     IDashboardAppService dashboardAppService) : IIntegrationEventHandler<DashboardResponseStatisticQueryEto>
// {
//     private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     private readonly IDashboardAppService _dashboardAppService = dashboardAppService ?? throw new ArgumentNullException(nameof(dashboardAppService));
//
//     public async Task HandleAsync(MessageEnvelope<DashboardResponseStatisticQueryEto> @event)
//     {
//         _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
//             nameof(DashboardResponseStatisticQueryEto)[..^"Eto".Length],
//             @event.CorrelationId ?? string.Empty,
//             @event.MessageId.ToString(),
//             @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);
//
//         _dashboardAppService.SetParentIntegrationEvent(@event);
//         await _dashboardAppService.DashboardResponseStatisticQueryAsync(@event.Message);
//     }
// }