namespace Hhs.Shared.Contracts.Cache;

public sealed class CachePermissionGrant
{
    public string Name { get; set; }
    public string ProviderName { get; set; }
    public string ProviderKey { get; set; }

    private CachePermissionGrant()
    {
        Name = string.Empty;
        ProviderName = string.Empty;
        ProviderKey = string.Empty;
    }

    public CachePermissionGrant(string name, string providerName, string providerKey) : this()
    {
        Name = name ?? string.Empty;
        ProviderName = providerName ?? string.Empty;
        ProviderKey = providerKey ?? string.Empty;
    }
}