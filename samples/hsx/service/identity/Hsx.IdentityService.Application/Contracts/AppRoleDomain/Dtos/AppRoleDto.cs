using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;

public sealed class AppRoleDto : AuditedEntityDto<Guid>
{
    public Guid TenantId { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsStatic { get; init; }
}
