namespace HsnSoft.Base.Authorization.Permissions.Store;

public sealed class PermissionConstraintAssignment
{
    public string Constraint { get; init; }

    public string ProviderName { get; init; }

    public string ProviderKey { get; init; }

    public string Value { get; init; }
}

// Constraint   : invoice.create.max-amount
// ProviderName : R
// ProviderKey  : sales
// Value        : 10000