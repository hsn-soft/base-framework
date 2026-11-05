using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;

/// <summary>What the roles are granted. A change is all of it or none, never beyond what the caller has, and it is written to the store the services read.</summary>
public interface IRolePermissionAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such role, or it is another tenant's.</exception>
    Task<RolePermissionsDto> GetAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Replaces what the role is granted with exactly the grants given, and answers what it has now.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such role, or a code is not an active permission.</exception>
    /// <exception cref="Hsx.IdentityService.Domain.PermissionDomain.Exceptions.PermissionValueRequiredException">A constraint has no value.</exception>
    /// <exception cref="Hsx.IdentityService.Domain.PermissionDomain.Exceptions.AppRolePermissionNotGrantableException">The caller does not have what it grants.</exception>
    Task<RolePermissionsDto> SetAsync(Guid roleId, SetRolePermissionsDto input, CancellationToken cancellationToken = default);
}
