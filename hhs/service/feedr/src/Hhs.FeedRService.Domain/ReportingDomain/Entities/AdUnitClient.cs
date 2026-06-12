using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.FeedRService.Domain.ReportingDomain.Entities;

/// <summary>
/// Per-client ad unit under an AdUnitTopLevel. This is the lowest level of the hierarchy
/// and is the granularity at which daily report aggregates are stored.
/// </summary>
public sealed class AdUnitClient : AuditedEntity<Guid>, IMultiTenant
{
    public Guid TenantId { get; private set; }

    public Guid AdUnitTopLevelId { get; private set; }
    public AdUnitTopLevel AdUnitTopLevel { get; private set; }

    /// <summary>The Google Ad Manager AdUnitId (client-specific).</summary>
    public string AdUnitCode { get; private set; } = string.Empty;

    /// <summary>Client identifier that owns this ad unit.</summary>
    public Guid ClientId { get; private set; }
    public string ClientName { get; private set; } = string.Empty;

    public ICollection<DashboardResponse> DailyReports { get; private set; } = new List<DashboardResponse>();

    private AdUnitClient() { }

    public AdUnitClient(Guid id, Guid tenantId, Guid adUnitTopLevelId, string adUnitCode, Guid clientId, string clientName)
    {
        Id = id;
        TenantId = tenantId;
        AdUnitTopLevelId = adUnitTopLevelId;
        AdUnitCode = adUnitCode ?? string.Empty;
        ClientId = clientId;
        ClientName = clientName ?? string.Empty;
    }

    public void UpdateClientMetadata(Guid clientId, string clientName)
    {
        ClientId = clientId;
        ClientName = clientName ?? string.Empty;
    }
}
