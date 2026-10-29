using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Exceptions;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using HsnSoft.Base.Application;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Edx.IdentityService.Application.Services;

public sealed class RolePermissionAppService(IRepository<Role, Guid> roles, IPermissionGrantRepository grants, PermissionCacheSynchronizer synchronizer, IServiceProvider services)
    : ApplicationService(services), IRolePermissionAppService
{
    public async Task<List<GrantedPermissionDto>> GetAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        _ = await FindAsync(roleId, cancellationToken).ConfigureAwait(false);
        return GrantMapping.ToDtos(await grants.GetRoleGrantsAsync(roleId, cancellationToken).ConfigureAwait(false));
    }

    public async Task SetAsync(Guid roleId, SetGrantsDto input, CancellationToken cancellationToken = default)
    {
        // a role that is not the caller's tenant's is not found (the tenant filter), so a grant can only be set where the caller may
        Role role = await FindAsync(roleId, cancellationToken).ConfigureAwait(false);
        List<(Permission Permission, string? Value)> resolved = await GrantMapping.ResolveAsync(grants, input, cancellationToken).ConfigureAwait(false);
        await grants.ReplaceRoleGrantsAsync(role, resolved, cancellationToken).ConfigureAwait(false);
        await synchronizer.SyncRoleAsync(role, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("The grants of the role {RoleId} ({RoleName}) were set: {GrantCount} permissions", role.Id, role.Name, resolved.Count);
    }

    private async Task<Role> FindAsync(Guid roleId, CancellationToken cancellationToken)
        => await roles.FindAsync(roleId, cancellationToken).ConfigureAwait(false) ?? throw RoleNotFound.For(roleId);
}
