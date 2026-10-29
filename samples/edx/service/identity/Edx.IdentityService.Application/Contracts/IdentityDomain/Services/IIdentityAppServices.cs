using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Edx.IdentityService.Application.Contracts.IdentityDomain.Services;

/// <summary>The users the caller may see: a user of a customer sees the users of its own tenant, a user of the system tenant sees all.</summary>
public interface IUserAppService
{
    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>Makes a user in the tenant of the caller, with the roles given (roles of that tenant).</summary>
    Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>Deletes a user (not the caller itself).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>The roles the caller may see, by the same rule.</summary>
public interface IRoleAppService
{
    Task<List<RoleDto>> GetFilterListAsync(GetRolesFilter filterInput, CancellationToken cancellationToken = default);

    Task<RoleDto> CreateAsync(RoleCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>Deletes a role, with its grants, and takes its entry out of the store the services read. A role a user has is not deleted.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>The tenants the caller may see: a user of a customer sees its own tenant only, a user of the system tenant sees all.</summary>
public interface ITenantAppService
{
    Task<List<TenantDto>> GetFilterListAsync(GetTenantsFilter filterInput, CancellationToken cancellationToken = default);
}

/// <summary>The catalogue of the permissions of the system (read only: the code is the list).</summary>
public interface IPermissionAppService
{
    Task<List<PermissionDto>> GetFilterListAsync(GetPermissionsFilter filterInput, CancellationToken cancellationToken = default);
}

/// <summary>What the roles of a tenant are granted. A caller reaches the roles of its own tenant; a caller of the system tenant reaches every tenant's.</summary>
public interface IRolePermissionAppService
{
    Task<List<GrantedPermissionDto>> GetAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Replaces what the role is granted and writes it to the store the services read, so it is seen by every service within moments.</summary>
    Task SetAsync(Guid roleId, SetGrantsDto input, CancellationToken cancellationToken = default);
}

/// <summary>What the caller itself can do: its grants and the grants of its roles, for a client that shows or hides what the user may use.</summary>
public interface ISessionPermissionAppService
{
    Task<List<GrantedPermissionDto>> GetMineAsync(CancellationToken cancellationToken = default);
}

/// <summary>The menu of the caller: the items it may see, as a tree, each page with what it needs and whether the caller has it.</summary>
public interface IMenuAppService
{
    Task<List<MenuDto>> GetMyMenusAsync(CancellationToken cancellationToken = default);
}
