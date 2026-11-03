namespace Hsx.IdentityService.Domain.AppRoleDomain.Entities;

/// <summary>Which subscriptions, and so which data, the users of a role see. The permissions of a role (which actions) are another thing.</summary>
public enum AppRoleType
{
    /// <summary>The roles of the system tenant: everything (the system tenant is under no isolation), so it has no links.</summary>
    System = 1,

    /// <summary>A role of a reseller that sees every subscription of the tenant.</summary>
    Tenant = 2,

    /// <summary>A role of a reseller that sees the subscriptions of the tenant for one company, which the role names.</summary>
    Company = 3,

    /// <summary>A role of a reseller that sees the subscriptions of the tenant for one customer, which the role names.</summary>
    Customer = 4,
}
