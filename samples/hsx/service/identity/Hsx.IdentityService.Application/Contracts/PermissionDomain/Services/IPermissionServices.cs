using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;

/// <summary>The catalogue of permissions (the code of the system is its only author: it is read here, never written).</summary>
public interface IPermissionAppService
{
    Task<List<PermissionDto>> GetFilterListAsync(GetPermissionsFilter filterInput, CancellationToken cancellationToken = default);
}

/// <summary>What the roles are granted.</summary>
public interface IRolePermissionAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such role, or it is another tenant's.</exception>
    Task<List<GrantedPermissionDto>> GetAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Replaces what the role is granted with exactly the permissions given, and writes it to the store the services read.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such role, or a code is not an active permission.</exception>
    Task SetAsync(Guid roleId, SetRolePermissionsDto input, CancellationToken cancellationToken = default);
}

/// <summary>What the caller was granted, through its roles.</summary>
public interface ISessionPermissionAppService
{
    Task<List<GrantedPermissionDto>> GetMineAsync(CancellationToken cancellationToken = default);
}
