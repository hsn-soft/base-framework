using Hhs.FeedRService.Domain.ConfigurationDomain.Models;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;

/// <summary>
/// MongoDB document representing a Google Ad Manager network and its full hierarchy:
/// Network → TopLevelGroups → Clients (AdUnitIds).
/// One document per (TenantId, NetworkCode) pair.
/// </summary>
public sealed class NetworkConfiguration : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid NetworkId { get; private set; }
    public Guid TenantId { get; private set; }
    [NotNull]
    public string NetworkCode { get; private set; } = string.Empty;
    [NotNull]
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public List<TopLevelGroupConfig> TopLevelGroups { get; private set; } = new();

    private NetworkConfiguration() { }

    public NetworkConfiguration(
        Guid networkId,
        Guid tenantId,
        string networkCode,
        string displayName,
        bool isActive,
        List<TopLevelGroupConfig> topLevelGroups)
    {
        Id = Guid.NewGuid();
        NetworkId = networkId;
        TenantId = tenantId;
        NetworkCode = networkCode ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        IsActive = isActive;
        TopLevelGroups = topLevelGroups ?? new List<TopLevelGroupConfig>();
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void UpdateTopLevelGroups(List<TopLevelGroupConfig> topLevelGroups)
    {
        TopLevelGroups = topLevelGroups ?? new List<TopLevelGroupConfig>();
    }
}
