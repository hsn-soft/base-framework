using Edx.IdentityService.Domain.IdentityDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;

namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>
/// An item of the menu of a client (the back office, the mobile app). The menu is the system's, not a tenant's, and is defined in the code (the identity service keeps the table in step at every start, like the permissions). A <see cref="MenuNodeType.Leaf"/> has a route and
/// at least one <see cref="MenuPermissionRelationType.Visibility"/> permission: a leaf without one is shown to nobody. A node or a caption has no route and is shown when something under it (after it, for a caption) is.
/// </summary>
public sealed class Menu : Entity<Guid>
{
    // for EF Core
    private Menu()
    {
    }

    public Guid? ParentId { get; private set; }

    /// <summary>What the client finds the item by (<c>invoice-management.invoices</c>): unique.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>The path of the page in the client; only a leaf has one.</summary>
    public string? Route { get; private set; }

    public string? Icon { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public MenuChannelType ChannelType { get; private set; }

    public MenuDockType DockType { get; private set; }

    public MenuNodeType NodeType { get; private set; }

    public static Menu Create(string code, MenuChannelType channelType, MenuDockType dockType, MenuNodeType nodeType, Guid? parentId, DateTimeOffset createdAt)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), IdentityConsts.MenuCodeMaxLength),
            ChannelType = channelType,
            DockType = dockType,
            NodeType = nodeType,
            ParentId = parentId,
            CreatedAt = createdAt,
            Title = code,
            IsActive = true,
        };

    /// <summary>Sets what the code says of the item. A caption is only at the root, a leaf has a route and a caption or a node has none.</summary>
    public void Define(string title, string? route, string? icon, int sortOrder, Guid? parentId, bool isActive)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), IdentityConsts.MenuTitleMaxLength);
        Route = NodeType == MenuNodeType.Leaf
            ? Guard.NotNullOrWhiteSpace(route, nameof(Route), IdentityConsts.MenuRouteMaxLength)
            : string.IsNullOrWhiteSpace(route) ? null : throw new DomainException($"The menu item '{Code}' is not a leaf: it has no route.").WithKey("menu_route_not_allowed", Code);
        Icon = string.IsNullOrWhiteSpace(icon) ? null : Guard.Length(icon.Trim(), nameof(Icon), IdentityConsts.MenuIconMaxLength);
        SortOrder = sortOrder;
        ParentId = NodeType == MenuNodeType.Caption && parentId is not null ? throw new DomainException($"The caption '{Code}' is only at the root.").WithKey("menu_caption_not_at_root", Code) : parentId;
        IsActive = isActive;
    }
}
