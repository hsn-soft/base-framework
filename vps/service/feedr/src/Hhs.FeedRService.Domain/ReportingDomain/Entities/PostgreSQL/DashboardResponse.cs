using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;

/// <summary>
/// Derived per-client, per-day aggregation of Google Ad Manager report data.
/// One record exists per (<see cref="AdUnitClientId"/>, <see cref="ReportDate"/>, <see cref="DemandChannel"/>,
/// <see cref="DemandSubchannelName"/>, <see cref="OrderId"/>) tuple.
/// Aggregatable metrics (impressions, revenue, etc.) are summed; <see cref="AverageEcpm"/>
/// is an impressions-weighted average of the source rows.
/// </summary>
public sealed class DashboardResponse : AuditedEntity<Guid>, IMultiTenant
{
    public Guid TenantId { get; private set; }

    public Guid AdUnitClientId { get; private set; }
    public AdUnitClient AdUnitClient { get; private set; }

    public Guid ClientId { get; private set; }

    /// <summary>The calendar day the aggregated metrics apply to.</summary>
    public DateTime ReportDate { get; private set; }

    /// <summary>Demand channel dimension (independent attribute eligible for all networks/adUnits).</summary>
    public string DemandChannel { get; private set; } = string.Empty;

    /// <summary>Demand subchannel name dimension (independent attribute eligible for all networks/adUnits).</summary>
    public string DemandSubchannelName { get; private set; } = string.Empty;

    /// <summary>Order ID (valid for all adUnitIds in the network).</summary>
    public string OrderId { get; private set; } = string.Empty;

    /// <summary>Order name.</summary>
    public string OrderName { get; private set; } = string.Empty;

    // Aggregatable fields (daily totals)
    public long CodeServedCount { get; private set; }
    public long Impressions { get; private set; }
    public double Revenue { get; private set; }
    public long ActiveViewEligibleImpressions { get; private set; }

    // Average field (impressions-weighted)
    public double AverageEcpm { get; private set; }

    /// <summary>Count of source rows that were aggregated to produce this record.</summary>
    public int SourceRowCount { get; private set; }


    /// <summary>MongoDB ObjectId / Guid of the source <c>RawGoogleAdManagerResponse</c> document.</summary>
    public Guid SourceMongoDbId { get; private set; }

    private DashboardResponse() { }

    public DashboardResponse(
        Guid id,
        Guid tenantId,
        Guid adUnitClientId,
        Guid clientId,
        DateTime reportDate,
        string demandChannel,
        string demandSubchannelName,
        string orderId,
        string orderName,
        long codeServedCount,
        long impressions,
        double revenue,
        long activeViewEligibleImpressions,
        double averageEcpm,
        int sourceRowCount,
        Guid sourceMongoDbId)
    {
        Id = id;
        TenantId = tenantId;
        AdUnitClientId = adUnitClientId;
        ClientId = clientId;
        ReportDate = reportDate.Date;
        DemandChannel = demandChannel ?? string.Empty;
        DemandSubchannelName = demandSubchannelName ?? string.Empty;
        OrderId = orderId ?? string.Empty;
        OrderName = orderName ?? string.Empty;
        CodeServedCount = codeServedCount;
        Impressions = impressions;
        Revenue = revenue;
        ActiveViewEligibleImpressions = activeViewEligibleImpressions;
        AverageEcpm = averageEcpm;
        SourceRowCount = sourceRowCount;
        SourceMongoDbId = sourceMongoDbId;
    }

    /// <summary>
    /// Merge new values into the existing daily aggregate (used when late-arriving
    /// data for an already-aggregated day needs to be combined).
    /// </summary>
    public void MergeFrom(
        long codeServedCount,
        long impressions,
        double revenue,
        long activeViewEligibleImpressions,
        double averageEcpm,
        int sourceRowCount,
        Guid sourceMongoDbId)
    {
        // Impressions-weighted average update
        var totalImpressions = Impressions + impressions;
        if (totalImpressions > 0)
        {
            AverageEcpm = (AverageEcpm * Impressions + averageEcpm * impressions) / totalImpressions;
        }

        CodeServedCount += codeServedCount;
        Impressions = totalImpressions;
        Revenue += revenue;
        ActiveViewEligibleImpressions += activeViewEligibleImpressions;
        SourceRowCount += sourceRowCount;
        SourceMongoDbId = sourceMongoDbId;
    }
}
