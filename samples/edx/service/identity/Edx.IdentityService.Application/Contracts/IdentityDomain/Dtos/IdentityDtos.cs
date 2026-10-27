using HsnSoft.Base.Application.Dtos;

namespace Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;

public sealed class UserDto : CreationAuditedEntityDto<Guid>
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public Guid TenantId { get; init; }
}

public sealed class RoleDto : CreationAuditedEntityDto<Guid>
{
    public string Name { get; init; } = string.Empty;

    public Guid TenantId { get; init; }
}

public sealed class TenantDto : EntityDto<Guid>
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

/// <summary>A permission of the catalogue.</summary>
public sealed class PermissionDto : EntityDto<Guid>
{
    public string Code { get; init; } = string.Empty;

    /// <summary><c>Endpoint</c>, <c>Operation</c> or <c>Constraint</c>.</summary>
    public string Type { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

/// <summary>A permission that was granted to a role or a user, with the value a constraint carries.</summary>
public sealed class GrantedPermissionDto
{
    public string Code { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string? Value { get; init; }
}
