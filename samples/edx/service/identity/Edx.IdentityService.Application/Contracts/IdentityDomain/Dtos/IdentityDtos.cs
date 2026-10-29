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

    public string? Description { get; init; }

    public bool IsActive { get; init; }
}

/// <summary>A permission that was granted to a role or a user, with the value a constraint carries.</summary>
public sealed class GrantedPermissionDto
{
    public string Code { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string? Value { get; init; }
}

/// <summary>An item of the menu of the caller (only what it may see), with what its page needs: the page opens with its visibility permission, which is why the item is here at all.</summary>
public sealed class MenuDto
{
    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    /// <summary>The path of the page; only a leaf has one.</summary>
    public string? Route { get; init; }

    public string? Icon { get; init; }

    /// <summary><c>Caption</c>, <c>Node</c> or <c>Leaf</c>.</summary>
    public string NodeType { get; init; } = string.Empty;

    public IReadOnlyList<MenuDto> Children { get; init; } = [];

    /// <summary>The permissions of a leaf (its page): what each is to the page and whether the caller has it.</summary>
    public IReadOnlyList<MenuPermissionDto> Permissions { get; init; } = [];
}

public sealed class MenuPermissionDto
{
    public string Code { get; init; } = string.Empty;

    /// <summary><c>Page</c>, <c>Endpoint</c>, <c>Operation</c> or <c>Constraint</c>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary><c>Visibility</c>, <c>Required</c>, <c>Action</c>, <c>Operation</c> or <c>Constraint</c>.</summary>
    public string Relation { get; init; } = string.Empty;

    public bool Granted { get; init; }
}
