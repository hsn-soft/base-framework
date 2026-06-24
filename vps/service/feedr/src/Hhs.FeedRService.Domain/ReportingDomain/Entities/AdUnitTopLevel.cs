using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.FeedRService.Domain.ReportingDomain.Entities;

/// <summary>
/// Top-level ad unit grouping under a Network. Multiple <see cref="AdUnitClient"/> records
/// (each mapped to a different client) can be attached to a single AdUnitTopLevel.
/// Shared across tenants — TenantId lives on AdUnitClient and DailyReportResponse.
/// </summary>
public sealed class AdUnitTopLevel : AuditedEntity<Guid>
{
    public Guid AdNetworkId { get; private set; }
    public AdNetwork AdNetwork { get; private set; }
    [NotNull]
    public string AdUnitTopLevelCode { get; private set; } = string.Empty;

    public ICollection<AdUnitClient> Clients { get; private set; } = new List<AdUnitClient>();

    private AdUnitTopLevel() { }

    public AdUnitTopLevel(Guid id, Guid adNetworkId, string adUnitTopLevelCode)
    {
        Id = id;
        AdNetworkId = adNetworkId;
        AdUnitTopLevelCode = adUnitTopLevelCode ?? string.Empty;
    }
}
