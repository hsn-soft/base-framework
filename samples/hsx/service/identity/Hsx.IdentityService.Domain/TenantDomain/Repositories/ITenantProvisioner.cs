namespace Hsx.IdentityService.Domain.TenantDomain.Repositories;

/// <summary>The first administrator of a tenant that is made: a user of the tenant, with the role <c>Admin</c>.</summary>
public sealed record AdminUserSpec(string UserName, string Email, string DisplayName, string Password, string? LanguageCode);

/// <summary>What was made: the tenant, its administrator role and the administrator (when one was asked for).</summary>
public sealed record ProvisionedTenant(Guid TenantId, Guid RoleId, Guid? UserId);

/// <summary>
/// Makes a reseller with what it needs to be used, in one transaction: either all of it is there or none of it is. The tenant, its administrator role (a Tenant role, with the grants that role starts with) and the first administrator. What is checked first (names that are
/// taken, the password) is the caller's; this only writes.
/// </summary>
public interface ITenantProvisioner
{
    /// <summary>A reseller: a tenant at the root of the tree, with its administrator role and, when asked, its first administrator.</summary>
    Task<ProvisionedTenant> CreateResellerAsync(string title, string name, AdminUserSpec? admin, CancellationToken cancellationToken = default);
}
