using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;

/// <summary>What was made for a reseller. The password of its administrator is not here: the system made it and sent it, and no person sees it.</summary>
public sealed class ProvisionedTenantDto : IBaseDto
{
    public Guid TenantId { get; init; }

    /// <summary>The administrator role of the tenant (a Tenant role).</summary>
    public Guid RoleId { get; init; }

    /// <summary>The first administrator.</summary>
    public Guid UserId { get; init; }
}
