using Hhs.FeedRService.AdManager.Controllers.Base;
using Hhs.FeedRService.Application.Contracts.DashboardDomain;
using Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.FeedRService.AdManager.Controllers;

[Produces("application/json")]
[Area("feedr-admanager-service")]
[ApiController]
[Route("api/[area]/v1/commercial/dashboard")]

public sealed class DashboardController(
    IServiceProvider provider,
    IReportQueryService reportQueryService) : BaseServiceController(provider)
{
    /// <summary>
    /// Get aggregated daily report data for a specific AdUnitId within a date range.
    /// Returns daily breakdown plus date-range-level sum totals and impressions-weighted averages.
    /// </summary>
    [HttpPost("by-ad-unit")]
    [ProducesResponseType(typeof(AggregatedReportResultDto), StatusCodes.Status200OK)]
    public async Task<AggregatedReportResultDto> GetByAdUnitAsync(
        [FromBody] GetReportByAdUnitRangeDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetByAdUnitAndDateRangeAsync(request, cancellationToken);

    /// <summary>
    /// Get aggregated daily report data for a specific ClientId within a date range.
    /// </summary>
    [HttpPost("by-client")]
    [ProducesResponseType(typeof(AggregatedReportResultDto), StatusCodes.Status200OK)]
    public async Task<AggregatedReportResultDto> GetByClientAsync(
        [FromBody] GetReportByClientRangeDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetByClientAndDateRangeAsync(request, cancellationToken);

    /// <summary>
    /// Get aggregated daily report data for a specific Network within a date range.
    /// </summary>
    [HttpPost("by-network")]
    [ProducesResponseType(typeof(AggregatedReportResultDto), StatusCodes.Status200OK)]
    public async Task<AggregatedReportResultDto> GetByNetworkAsync(
        [FromBody] GetReportByNetworkRangeDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetByNetworkAndDateRangeAsync(request, cancellationToken);

    /// <summary>
    /// Get aggregated daily report data for a specific TopLevelGroup (AdUnitIdTopLevel) within a date range.
    /// </summary>
    [HttpPost("by-top-level")]
    [ProducesResponseType(typeof(AggregatedReportResultDto), StatusCodes.Status200OK)]
    public async Task<AggregatedReportResultDto> GetByTopLevelAsync(
        [FromBody] GetReportByTopLevelRangeDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetByTopLevelAndDateRangeAsync(request, cancellationToken);

    /// <summary>
    /// Get aggregated daily report data using a composite filter (any combination of Network, TopLevel, Client).
    /// At least one filter must be provided along with the date range.
    /// </summary>
    [HttpPost("by-filter")]
    [ProducesResponseType(typeof(AggregatedReportResultDto), StatusCodes.Status200OK)]
    public async Task<AggregatedReportResultDto> GetByCompositeFilterAsync(
        [FromBody] GetReportByCompositeFilterDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetByCompositeFilterAsync(request, cancellationToken);

    /// <summary>
    /// Get aggregated daily report data filtered by DemandChannel and DemandSubchannelName.
    /// DemandChannel and DemandSubchannelName are independent attributes eligible for all networks and adUnits.
    /// These can be used together in API filtering.
    /// </summary>
    [HttpPost("by-demand-channel")]
    [ProducesResponseType(typeof(AggregatedReportResultDto), StatusCodes.Status200OK)]
    public async Task<AggregatedReportResultDto> GetByDemandChannelAsync(
        [FromBody] GetReportByDemandChannelFilterDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetByDemandChannelFilterAsync(request, cancellationToken);

    /// <summary>
    /// Get aggregated daily report data filtered by Order.
    /// An Order is valid for all adUnitIds in the network.
    /// Order criteria can be used with network and adUnit in API filtering.
    /// </summary>
    [HttpPost("by-order")]
    [ProducesResponseType(typeof(AggregatedReportResultDto), StatusCodes.Status200OK)]
    public async Task<AggregatedReportResultDto> GetByOrderAsync(
        [FromBody] GetReportByOrderFilterDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetByOrderFilterAsync(request, cancellationToken);


    /// <summary>
    /// Returns distinct orders (OrderId + OrderName) found in the stored report data.
    /// At least one filter parameter (networkCode, adUnitTopLevelCode, adUnitCode, clientId) is recommended.
    /// </summary>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(List<OrderInfoDto>), StatusCodes.Status200OK)]
    public async Task<List<OrderInfoDto>> GetOrdersAsync(
        [FromBody] GetOrdersQueryDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetOrdersAsync(request, cancellationToken);

    /// <summary>
    /// Get report data grouped by DemandSubchannelName using composite filtering.
    /// Returns totals (revenue, impressions, code served) per subchannel.
    /// </summary>
    [HttpPost("grouped-by-subchannel")]
    [ProducesResponseType(typeof(GroupedBySubchannelResultDto), StatusCodes.Status200OK)]
    public async Task<GroupedBySubchannelResultDto> GetGroupedBySubchannelAsync(
        [FromBody] GetReportByCompositeFilterDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetGroupedBySubchannelAsync(request, cancellationToken);


    /// <summary>
    /// Get report data grouped by OrderName using composite filtering.
    /// Returns totals (revenue, impressions, code served) per order.
    /// </summary>
    [HttpPost("grouped-by-order")]
    [ProducesResponseType(typeof(GroupedByOrderResultDto), StatusCodes.Status200OK)]
    public async Task<GroupedByOrderResultDto> GetGroupedByOrderAsync(
        [FromBody] GetReportByCompositeFilterDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetGroupedByOrderAsync(request, cancellationToken);

    /// <summary>
    /// Get report data grouped by date using composite filtering.
    /// Returns daily revenue, impressions, and avg eCPM plus overall totals.
    /// </summary>
    [HttpPost("grouped-by-date")]
    [ProducesResponseType(typeof(GroupedByDateResultDto), StatusCodes.Status200OK)]
    public async Task<GroupedByDateResultDto> GetGroupedByDateAsync(
        [FromBody] GetReportByCompositeFilterDto request,
        CancellationToken cancellationToken)
        => await reportQueryService.GetGroupedByDateAsync(request, cancellationToken);
}
