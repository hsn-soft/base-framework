using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Exceptions;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Microsoft.Extensions.Logging;

namespace Hsx.IdentityService.Application.Services;

public sealed class RolePermissionAppService(IRepository<AppRole, Guid> roles, IPermissionGrantRepository grants, PermissionCacheSynchronizer synchronizer, IServiceProvider services)
    : ApplicationServiceBase(services), IRolePermissionAppService
{
    public async Task<List<GrantedPermissionDto>> GetAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        _ = await FindAsync(roleId, cancellationToken).ConfigureAwait(false);
        return GrantMapping.ToDtos(await grants.GetRoleGrantsAsync(roleId, cancellationToken).ConfigureAwait(false));
    }

    public async Task SetAsync(Guid roleId, SetRolePermissionsDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // a role that is not the caller's tenant's is not found (the tenant filter), so a grant can only be set where the caller may
        AppRole role = await FindAsync(roleId, cancellationToken).ConfigureAwait(false);

        // a code given twice is one grant; a code that is not an active permission of the catalogue stops the change before anything is written
        HashSet<string> asked = [.. input.Codes.Select(code => code.Trim().ToLowerInvariant())];
        IReadOnlyList<Permission> found = await grants.FindActiveByCodesAsync(asked, cancellationToken).ConfigureAwait(false);
        string? missing = asked.FirstOrDefault(code => found.All(permission => permission.Code != code));
        if (missing is not null)
        {
            throw PermissionNotFound.For(missing);
        }

        await grants.ReplaceRoleGrantsAsync(role, found, cancellationToken).ConfigureAwait(false);
        await synchronizer.SyncRoleAsync(role, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("The grants of the role {RoleId} ({RoleName}) were set: {GrantCount} permissions", role.Id, role.Name, found.Count);
    }

    private async Task<AppRole> FindAsync(Guid roleId, CancellationToken cancellationToken)
        => await roles.FindAsync(roleId, cancellationToken).ConfigureAwait(false) ?? throw AppRoleNotFound.For(roleId);
}
