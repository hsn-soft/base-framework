using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;

public sealed class AppRoleDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    [NotNull] public string Name { get; set; } = string.Empty;

    public bool IsStatic { get; set; }
    public bool IsDefault { get; set; }
}