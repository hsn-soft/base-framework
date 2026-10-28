using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;

namespace Edx.IdentityService.Application.Services;

/// <summary>
/// Writes what the roles were granted to the store the services read (the distributed cache), one entry for each role, under the scope of its tenant. The database is the truth and the store is its copy: a change of the grants of a role writes
/// that role's entry at once, and <see cref="SyncAllAsync"/> writes every entry again (when the service starts and every few minutes), so a cache that was lost or a write that failed is made right by the next round.
/// </summary>
public sealed class PermissionCacheSynchronizer(IPermissionGrantRepository grants, IPermissionStore store)
{
    public async Task SyncRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        IReadOnlyList<GrantedPermission> granted = await grants.GetRoleGrantsAsync(role.Id, cancellationToken).ConfigureAwait(false);
        await WriteAsync(role.TenantId, PermissionProviders.Role, role.Name, granted, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Takes the entry of a role away (the role was deleted): its grants are nothing.</summary>
    public Task RemoveRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        return store.RemoveAsync(EdxPermissionScopes.ForTenant(role.TenantId), new PermissionSubject(PermissionProviders.Role, role.Name), cancellationToken);
    }

    /// <summary>Writes the entry of every role of every tenant.</summary>
    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (SubjectGrants role in await grants.GetAllRoleSubjectsAsync(cancellationToken).ConfigureAwait(false))
        {
            await WriteAsync(role.TenantId, PermissionProviders.Role, role.Key, role.Grants, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task WriteAsync(Guid tenantId, string provider, string key, IReadOnlyList<GrantedPermission> granted, CancellationToken cancellationToken)
        => store.SetAsync(
            EdxPermissionScopes.ForTenant(tenantId),
            new PermissionSubject(provider, key),
            new PermissionGrants(granted.ToDictionary(item => item.Code, item => item.Value)),
            cancellationToken);
}
