namespace Hhs.Shared.Contracts.Cache;

/// <summary>
/// Permission   : invoice.create
/// ProviderName : R
/// ProviderKey  : sales
/// </summary>
public sealed class CachePermissionAssignment
{
    public string Permission { get; set; }
    public string ProviderName { get; set; }
    public string ProviderKey { get; set; }

    private CachePermissionAssignment()
    {
        Permission = string.Empty;
        ProviderName = string.Empty;
        ProviderKey = string.Empty;
    }

    public CachePermissionAssignment(string permission, string providerName, string providerKey) : this()
    {
        Permission = permission ?? string.Empty;
        ProviderName = providerName ?? string.Empty;
        ProviderKey = providerKey ?? string.Empty;
    }
}