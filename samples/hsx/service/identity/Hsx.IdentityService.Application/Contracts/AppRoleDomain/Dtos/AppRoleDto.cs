using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Domain.Enums;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;

public sealed class AppRoleDto : AuditedEntityDto<Guid>
{
    public Guid TenantId { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsStatic { get; init; }

    /// <summary><c>System</c>, <c>Tenant</c>, <c>Company</c> or <c>Customer</c>: which subscriptions, so which data, its users see.</summary>
    public AppRoleType RoleType { get; init; }

    public Guid? CompanyId { get; init; }

    public Guid? CustomerId { get; init; }
}
