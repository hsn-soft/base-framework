namespace Hsx.Shared.Helper;

/// <summary>What a tenant is.</summary>
public enum TenantTypes
{
    Unknown = 0,

    /// <summary>The tenant of the people who run the system.</summary>
    System = 1,

    /// <summary>A company that sells the products to customers (and may have accounts under it).</summary>
    Reseller = 2,

    /// <summary>A tenant under a reseller.</summary>
    Account = 3,
}
