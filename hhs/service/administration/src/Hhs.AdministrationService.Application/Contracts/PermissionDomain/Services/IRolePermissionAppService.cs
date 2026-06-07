using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;

public interface IRolePermissionAppService
{
    Task<RolePermissionsDto> GetRolePermissionsAsync(GetRolePermissionsFilter filter);

    Task SetRolePermissionsAsync(RolePermissionsDto filter);
}