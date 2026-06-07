using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;

public sealed class AppRoleDto : EntityDto<Guid>
{
    public Guid TenantId { get; set; }
    [CanBeNull] public string TenantName { get; set; }

    [NotNull] public string Name { get; set; } = string.Empty;

    public bool IsStatic { get; set; }
    public bool IsDefault { get; set; }
}