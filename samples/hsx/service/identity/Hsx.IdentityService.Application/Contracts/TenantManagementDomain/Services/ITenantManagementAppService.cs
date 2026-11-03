using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;

namespace Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;

/// <summary>What the system tenant does to have resellers. The companies, the customers and the sales are the catalogue and the subscriptions it writes with their own endpoints.</summary>
public interface ITenantManagementAppService
{
    /// <summary>A reseller, with its administrator role (a Tenant role) and (when given) its first administrator. Only the system tenant.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The caller is not the system tenant (403).</exception>
    Task<ProvisionedTenantDto> CreateResellerAsync(CreateResellerDto input, CancellationToken cancellationToken = default);
}
