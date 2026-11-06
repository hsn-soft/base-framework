using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.TenantDomain.Services;

/// <summary>What can be done with the tenants. A tenant sees the tenants under it (and itself); the system tenant sees all of them.</summary>
public interface ITenantAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such tenant, or the caller may not see it.</exception>
    Task<TenantDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<TenantDto>> GetPagedListAsync(GetTenantsPagedDto pagedInput, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.TenantDomain.Exceptions.TenantNameDuplicateException">There is a tenant with this name.</exception>
    Task<TenantDto> CreateAsync(TenantCreateDto input, CancellationToken cancellationToken = default);

    Task<TenantDto> UpdateAsync(TenantUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.TenantDomain.Exceptions.TenantLoopException">The new parent is the tenant or under it.</exception>
    Task<TenantDto> MoveAsync(TenantMoveDto input, CancellationToken cancellationToken = default);

    /// <exception cref="Hsx.IdentityService.Domain.TenantDomain.Exceptions.TenantNotEmptyException">The tenant has tenants or users.</exception>
    Task<TenantDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
