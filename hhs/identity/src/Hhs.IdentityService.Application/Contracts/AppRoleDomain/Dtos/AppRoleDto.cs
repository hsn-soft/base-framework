using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos;

public sealed class AppRoleDto
{
    public Guid Id { get; set; }

    [CanBeNull]
    public string Name { get; set; }

    // Custom identity model
    public Guid? TenantId { get; set; }
    [CanBeNull]
    public string TenantDomain { get; set; }

    public bool IsDefault { get; set; }
    public bool IsStatic { get; set; }
    public bool IsPublic { get; set; }
}