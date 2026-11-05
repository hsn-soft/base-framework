using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Services;

/// <summary>What can be done with the roles of a tenant. The subscriptions a role has follow its type and are kept by the system (they can be read, not edited).</summary>
public interface IAppRoleAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such role, or the caller may not see it.</exception>
    Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPagedDto pagedInput, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.AppRoleDomain.Exceptions.AppRoleNameDuplicateException">The tenant has a role with this name.</exception>
    /// <exception cref="Hsx.IdentityService.Domain.AppRoleDomain.Exceptions.AppRoleTypeInvalidException">The type does not fit the tenant, or the company or the customer is not one the tenant sold to.</exception>
    Task<AppRoleDto> CreateAsync(AppRoleCreateDto input, CancellationToken cancellationToken = default);

    Task<AppRoleDto> UpdateAsync(AppRoleUpdateDto input, CancellationToken cancellationToken = default);

    Task<AppRoleSubscriptionsDto> GetSubscriptionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the role and answers it as it was.</summary>
    /// <exception cref="Hsx.IdentityService.Domain.AppRoleDomain.Exceptions.AppRoleNotDeletableException">The role is static or has users.</exception>
    Task<AppRoleDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
