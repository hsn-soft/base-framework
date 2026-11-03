namespace Hsx.Shared.Helper;

/// <summary>What a tenant is. A company and a customer are not tenants.</summary>
public enum TenantTypes
{
    Unknown = 0,

    /// <summary>The tenant of the people who run the system.</summary>
    System = 1,

    /// <summary>A company that sells the products to customers.</summary>
    Reseller = 2,
}
