namespace Hsx.IdentityService.Domain.TenantDomain.Repositories;

/// <summary>The first administrator of a tenant that is made: a user of the tenant, with the role <c>Admin</c>.</summary>
public sealed record AdminUserSpec(string UserName, string Email, string DisplayName, string Password, string? LanguageCode);

/// <summary>What was made: the tenant, its administrator role, the administrator (when one was asked for) and the company (when a company was made with the tenant).</summary>
public sealed record ProvisionedTenant(Guid TenantId, Guid RoleId, Guid? UserId, Guid? CompanyId);

/// <summary>
/// Makes a tenant with what it needs to be used, in one transaction: either all of it is there or none of it is. The tenant, its administrator role with the grants that role starts with, and the first administrator. What is checked first (names that are
/// taken, the password) is the caller's; this only writes.
/// </summary>
public interface ITenantProvisioner
{
    /// <summary>A reseller: a tenant at the root of the tree, with its administrator role and, when asked, its first administrator.</summary>
    Task<ProvisionedTenant> CreateResellerAsync(string title, string name, AdminUserSpec? admin, CancellationToken cancellationToken = default);

    /// <summary>A company of a reseller with the account tenant under that reseller where its people sign in (the company keeps the link), the administrator role of the account and, when asked, its first administrator. The company belongs to the reseller.</summary>
    Task<ProvisionedTenant> CreateCompanyAsync(Guid resellerTenantId, string title, string name, AdminUserSpec? admin, CancellationToken cancellationToken = default);
}
