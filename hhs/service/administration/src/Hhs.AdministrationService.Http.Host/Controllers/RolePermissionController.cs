using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Controllers.Base;
using Hhs.Shared.Helper.Consts.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.AdministrationService.Controllers;

[Route("api/administration-service/v1/commercial/role-permissions")]
public sealed class RolePermissionController : BaseServiceController
{
    private readonly IRolePermissionAppService _rolePermissionAppService;

    public RolePermissionController(IServiceProvider provider,
        IRolePermissionAppService rolePermissionAppService) : base(provider)
    {
        _rolePermissionAppService = rolePermissionAppService;
    }

    [Authorize(AdministrationServicePermissions.RolePermissions.Read)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<RolePermissionsDto> GetRolePermissionsAsync(GetRolePermissionsFilter filter)
    {
        // if (CurrentUser.TenantId.HasValue)
        // {
        //     // check tenant access system roles
        //     if (filter is { RoleUniqueName: not null } && !filter.RoleUniqueName.Contains($"#{CurrentUser.TenantDomain}"))
        //     {
        //         throw new BaseHttpException((int)HttpStatusCode.Forbidden, L[AdministrationServiceErrorCodes.UnauthorizedTenantError]);
        //     }
        // }
        // else if (!CurrentUser.TenantDomain.Equals(DefaultDomainNames.System))
        // {
        //     throw new BaseHttpException((int)HttpStatusCode.Forbidden, L[AdministrationServiceErrorCodes.UnauthorizedTenantError]);
        // }

        return await _rolePermissionAppService.GetRolePermissionsAsync(filter);
    }

    [Authorize(AdministrationServicePermissions.RolePermissions.Update)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task SetRolePermissionsAsync([FromBody] RolePermissionsDto filter)
    {
        // if (CurrentUser.TenantId.HasValue)
        // {
        //     // check tenant access system roles
        //     if (filter is { RoleUniqueName: not null } && !filter.RoleUniqueName.Contains($"#{CurrentUser.TenantDomain}"))
        //     {
        //         throw new BaseHttpException((int)HttpStatusCode.Forbidden, L[AdministrationServiceErrorCodes.UnauthorizedTenantError]);
        //     }
        // }
        // else if (!CurrentUser.TenantDomain.Equals(DefaultDomainNames.System))
        // {
        //     throw new BaseHttpException((int)HttpStatusCode.Forbidden, L[AdministrationServiceErrorCodes.UnauthorizedTenantError]);
        // }

        await _rolePermissionAppService.SetRolePermissionsAsync(filter);
    }
}