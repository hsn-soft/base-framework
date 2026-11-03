namespace Hsx.IdentityService.Application.Contracts.MenuDomain.Dtos;

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

    /// <summary><c>Page</c>, <c>Endpoint</c> or <c>Operation</c>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary><c>Visibility</c>, <c>Required</c>, <c>Action</c> or <c>Operation</c>.</summary>
    public string Relation { get; init; } = string.Empty;

    public bool Granted { get; init; }
}
