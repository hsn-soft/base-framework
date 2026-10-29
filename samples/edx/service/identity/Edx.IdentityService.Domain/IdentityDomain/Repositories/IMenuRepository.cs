using Edx.IdentityService.Domain.IdentityDomain.Entities;

namespace Edx.IdentityService.Domain.IdentityDomain.Repositories;

/// <summary>A permission of a menu item and what it is to the item.</summary>
public sealed record MenuPermissionInfo(string Code, PermissionType Type, MenuPermissionRelationType Relation, int SortOrder);

/// <summary>A menu item with its permissions.</summary>
public sealed record MenuWithPermissions(Menu Menu, IReadOnlyList<MenuPermissionInfo> Permissions);

/// <summary>Where the menus are kept. The menu is the system's, so no tenant filter is on it.</summary>
public interface IMenuRepository
{
    /// <summary>The active items of a client's menu in a place, with their permissions, in no tree: the parent of an item is its <see cref="Menu.ParentId"/>.</summary>
    Task<IReadOnlyList<MenuWithPermissions>> GetActiveAsync(MenuChannelType channel, MenuDockType dock, CancellationToken cancellationToken = default);
}
