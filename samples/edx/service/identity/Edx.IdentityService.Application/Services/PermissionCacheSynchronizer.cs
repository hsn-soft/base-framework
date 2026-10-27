using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;

namespace Edx.IdentityService.Application.Services;

/// <summary>
/// Writes what the roles and the users were granted to the store the services read (the distributed cache), one entry for each, under the scope of its tenant. The database is the truth and the store is its copy: a change of the grants of a role writes
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

    public async Task SyncUserAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        IReadOnlyList<GrantedPermission> granted = await grants.GetUserGrantsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        await WriteAsync(user.TenantId, PermissionProviders.User, user.Id.ToString(), granted, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes the entry of every role and of every user that has grants of its own, in every tenant.</summary>
    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (SubjectGrants role in await grants.GetAllRoleSubjectsAsync(cancellationToken).ConfigureAwait(false))
        {
            await WriteAsync(role.TenantId, PermissionProviders.Role, role.Key, role.Grants, cancellationToken).ConfigureAwait(false);
        }

        foreach (SubjectGrants user in await grants.GetAllUserSubjectsAsync(cancellationToken).ConfigureAwait(false))
        {
            await WriteAsync(user.TenantId, PermissionProviders.User, user.Key, user.Grants, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task WriteAsync(Guid tenantId, string provider, string key, IReadOnlyList<GrantedPermission> granted, CancellationToken cancellationToken)
        => store.SetAsync(
            EdxPermissionScopes.ForTenant(tenantId),
            new PermissionSubject(provider, key),
            new PermissionGrants(granted.ToDictionary(item => item.Code, item => item.Value)),
            cancellationToken);
}
