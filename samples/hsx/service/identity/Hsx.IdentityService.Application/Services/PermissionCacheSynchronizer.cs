using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// Writes what the roles were granted to the store the services read (the distributed cache), one entry for each role, under the scope of its tenant, with the value of each constraint. The database is the truth and the store is its copy: a change of the grants of a
/// role writes that role's entry once, and <see cref="SyncAllAsync"/> writes every entry again (when the service starts and every few minutes, from the host), so a cache that was lost or a write that failed is made right by the next round.
/// </summary>
public sealed class PermissionCacheSynchronizer(IPermissionGrantRepository grants, IPermissionStore store)
{
    public async Task SyncRoleAsync(AppRole role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        await SyncRoleAsync(role.Id, role.TenantId, role.Name, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The same for a role known by its id, its tenant and its name (the administrator role of a tenant that was just made).</summary>
    public async Task SyncRoleAsync(Guid roleId, Guid tenantId, string roleName, CancellationToken cancellationToken = default)
        => await WriteAsync(tenantId, roleName, await grants.GetRoleGrantsAsync(roleId, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    /// <summary>Takes the entry of a role away (the role was deleted, or it has another name now): its grants under that name are nothing.</summary>
    public Task RemoveRoleAsync(Guid tenantId, string roleName, CancellationToken cancellationToken = default)
        => store.RemoveAsync(HsxPermissionScopes.ForTenant(tenantId), new PermissionSubject(PermissionProviders.Role, roleName), cancellationToken);

    /// <summary>Writes the entry of every role of every tenant.</summary>
    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (SubjectGrants role in await grants.GetAllRoleSubjectsAsync(cancellationToken).ConfigureAwait(false))
        {
            await WriteAsync(role.TenantId, role.Key, role.Grants, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task WriteAsync(Guid tenantId, string roleName, IReadOnlyList<GrantedPermission> granted, CancellationToken cancellationToken)
        => store.SetAsync(
            HsxPermissionScopes.ForTenant(tenantId),
            new PermissionSubject(PermissionProviders.Role, roleName),
            new PermissionGrants(granted.ToDictionary(item => item.Code, item => item.Value)),
            cancellationToken);
}
