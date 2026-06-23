using Hhs.FeedRService.Application.Contracts.DashboardDomain.Dtos;

namespace Hhs.FeedRService.Application.Contracts.DashboardDomain;

public interface IReportPersistenceService
{
    /// <summary>
    /// Stage 1: Persist the raw Google Ad Manager NDJSON response into MongoDB
    /// as a single bulk document and return its id.
    /// </summary>
    Task<Guid> PersistRawResponseAsync(
        Guid tenantId,
        string requestId,
        string jobName,
        string network,
        string adUnitIdTopLevel,
        string adUnitId,
        DateTime reportDate,
        string rawResponseBody,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stage 2: Derive / transform a single raw MongoDB response document into
    /// the normalized per-client, per-day PostgreSQL records.
    /// </summary>
    Task DeriveToPostgresAsync(Guid rawResponseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch variant of <see cref="DeriveToPostgresAsync"/> - scans MongoDB for
    /// unprocessed responses and derives each to PostgreSQL.
    /// </summary>
    Task<int> DerivePendingReportsAsync(int maxCount = 50, CancellationToken cancellationToken = default);
}

public interface IReportQueryService
{
    Task<AggregatedReportResultDto> GetByAdUnitAndDateRangeAsync(GetReportByAdUnitRangeDto input, CancellationToken cancellationToken = default);
    Task<AggregatedReportResultDto> GetByClientAndDateRangeAsync(GetReportByClientRangeDto input, CancellationToken cancellationToken = default);
    Task<AggregatedReportResultDto> GetByNetworkAndDateRangeAsync(GetReportByNetworkRangeDto input, CancellationToken cancellationToken = default);
    Task<AggregatedReportResultDto> GetByTopLevelAndDateRangeAsync(GetReportByTopLevelRangeDto input, CancellationToken cancellationToken = default);
    Task<AggregatedReportResultDto> GetByCompositeFilterAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets report data filtered by DemandChannel and DemandSubchannelName (independent attributes for all networks/adUnits).
    /// </summary>
    Task<AggregatedReportResultDto> GetByDemandChannelFilterAsync(GetReportByDemandChannelFilterDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets report data filtered by Order (valid for all adUnitIds in the network).
    /// Order criteria can be used with network and adUnit.
    /// </summary>
    Task<AggregatedReportResultDto> GetByOrderFilterAsync(GetReportByOrderFilterDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns distinct orders (OrderId + OrderName) seen in the stored report data,
    /// scoped by any combination of network, top-level, adUnit, or client.
    /// </summary>
    Task<List<OrderInfoDto>> GetOrdersAsync(GetOrdersQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns report data grouped by DemandSubchannelName using the composite filter.
    /// </summary>
    Task<GroupedBySubchannelResultDto> GetGroupedBySubchannelAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns report data grouped by OrderName using the composite filter.
    /// </summary>
    Task<GroupedByOrderResultDto> GetGroupedByOrderAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default);


    /// <summary>
    /// Returns report data grouped by date using the composite filter.
    /// Each item contains daily totals for revenue, impressions, and avg eCPM.
    /// </summary>
    Task<GroupedByDateResultDto> GetGroupedByDateAsync(GetReportByCompositeFilterDto input, CancellationToken cancellationToken = default);
}
