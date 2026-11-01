using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Services;

/// <summary>What can be done with the roles of a tenant and the subscriptions they have.</summary>
public interface IAppRoleAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such role, or the caller may not see it.</exception>
    Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPaged pagedInput, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.AppRoleDomain.Exceptions.AppRoleNameDuplicateException">The tenant has a role with this name.</exception>
    /// <exception cref="Hsx.IdentityService.Domain.AppRoleDomain.Exceptions.SubscriptionNotAllowedException">A subscription is not one the caller may give.</exception>
    Task<AppRoleDto> CreateAsync(AppRoleCreateDto input, CancellationToken cancellationToken = default);

    Task UpdateAsync(AppRoleUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>The ids of the subscriptions of a role.</summary>
    Task<List<Guid>> GetSubscriptionIdsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.AppRoleDomain.Exceptions.SubscriptionNotAllowedException">A subscription is not one the caller may give.</exception>
    Task SetSubscriptionsAsync(AppRoleSetSubscriptionsDto input, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.AppRoleDomain.Exceptions.AppRoleNotDeletableException">The role is static or has users.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
