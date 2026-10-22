namespace HsnSoft.Base.Authorization.Permissions.Store;

public sealed class PermissionAssignment
{
    public string Permission { get; init; }

    public string ProviderName { get; init; }

    public string ProviderKey { get; init; }
}

// Permission   : invoice.create
// ProviderName : R
// ProviderKey  : sales