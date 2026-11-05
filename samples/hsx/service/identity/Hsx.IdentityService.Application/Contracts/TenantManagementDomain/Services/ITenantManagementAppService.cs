using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos.Submits;

namespace Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;

/// <summary>What the system tenant does to start a reseller.</summary>
public interface ITenantManagementAppService
{
    /// <summary>Makes a reseller: its tenant, its administrator role with the starting grants, and its first administrator with a temporary password the system makes and the notification service sends. All of it or none.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The caller is not the system tenant (403).</exception>
    /// <exception cref="Hsx.IdentityService.Domain.TenantDomain.Exceptions.TenantNameDuplicateException">The name is taken.</exception>
    Task<ProvisionedTenantDto> CreateResellerAsync(CreateResellerDto input, CancellationToken cancellationToken = default);
}
