namespace Hhs.Shared.Contracts.Cache;

/// <summary>
/// Constraint   : invoice.create.max-amount
/// ProviderName : R
/// ProviderKey  : sales
/// Value        : 10000
/// </summary>
public sealed class CachePermissionConstraintAssignment
{
    public string Constraint { get; set; }
    public string ProviderName { get; set; }
    public string ProviderKey { get; set; }
    public string Value { get; set; }

    private CachePermissionConstraintAssignment()
    {
        Constraint = string.Empty;
        ProviderName = string.Empty;
        ProviderKey = string.Empty;
        Value = string.Empty;
    }

    public CachePermissionConstraintAssignment(string constraint, string providerName, string providerKey, string value) : this()
    {
        Constraint = constraint ?? string.Empty;
        ProviderName = providerName ?? string.Empty;
        ProviderKey = providerKey ?? string.Empty;
        Value = value ?? string.Empty;
    }
}