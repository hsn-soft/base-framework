using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.FeedRService.Domain.ReportingDomain.Repositories;

public interface IDashboardRepository : IReadOnlyGenericRepository<DashboardResponse, Guid>
{
    Task<AdNetwork> EnsureNetworkAsync(string networkCode, string displayName, CancellationToken cancellationToken = default);

    Task<AdUnitTopLevel> EnsureTopLevelAsync(Guid adNetworkId, string adUnitTopLevelCode, CancellationToken cancellationToken = default);

    Task<AdUnitClient> EnsureAdUnitClientAsync(Guid tenantId, Guid adUnitTopLevelId, string adUnitCode, Guid clientId, string clientName, CancellationToken cancellationToken = default);

    Task<DashboardResponse> UpsertDailyReportAsync(DashboardResponse draft, CancellationToken cancellationToken = default);

    Task InsertMappingAsync(MongoToPostgresMapping mapping, CancellationToken cancellationToken = default);

    Task<List<DashboardResponse>> GetByAdUnitAndDateRangeAsync(string adUnitCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<List<DashboardResponse>> GetByClientAndDateRangeAsync(Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<List<DashboardResponse>> GetByNetworkAndDateRangeAsync(string networkCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<List<DashboardResponse>> GetByTopLevelAndDateRangeAsync(string adUnitTopLevelCode, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<List<DashboardResponse>> GetByCompositeFilterAsync(string networkCode, string adUnitTopLevelCode, Guid? clientId, string demandChannel, string demandSubchannelName, string orderId, string orderName, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets report data filtered by DemandChannel and DemandSubchannelName (independent attributes for all networks/adUnits).
    /// </summary>
    Task<List<DashboardResponse>> GetByDemandChannelFilterAsync(
        string networkCode,
        string adUnitTopLevelCode,
        string demandChannel,
        string demandSubchannelName,
        Guid? clientId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets report data filtered by Order (valid for all adUnitIds in the network).
    /// Order criteria can be used with network and adUnit.
    /// </summary>
    Task<List<DashboardResponse>> GetByOrderFilterAsync(
        string networkCode,
        string orderId,
        string adUnitCode,
        Guid? clientId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the distinct (OrderId, OrderName) pairs seen for the given scope.
    /// At least one of networkCode, adUnitTopLevelCode, adUnitCode, or clientId should be non-null.
    /// </summary>
    Task<List<(string OrderId, string OrderName)>> GetDistinctOrdersAsync(
        string? networkCode,
        string? adUnitTopLevelCode,
        string? adUnitCode,
        Guid? clientId,
        CancellationToken cancellationToken = default);
}
