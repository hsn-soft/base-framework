using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.FeedRService.Domain.ReportingDomain.Entities;

/// <summary>
/// Google Ad Manager Network reference. A customer configuration may reference up to 10 of these.
/// Persisted in PostgreSQL via EF Core as part of the normalized hierarchy:
/// Network → AdUnitTopLevel → AdUnitClient → DailyReportResponse.
/// Networks are shared across tenants — TenantId lives on AdUnitClient and DailyReportResponse.
/// </summary>
public sealed class AdNetwork : AuditedEntity<Guid>
{
    [NotNull]
    public string NetworkCode { get; private set; } = string.Empty;
    [NotNull]
    public string DisplayName { get; private set; } = string.Empty;

    public ICollection<AdUnitTopLevel> TopLevelAdUnits { get; private set; } = new List<AdUnitTopLevel>();

    private AdNetwork() { }

    public AdNetwork(Guid id, string networkCode, string displayName)
    {
        Id = id;
        NetworkCode = networkCode ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName ?? string.Empty;
    }
}
