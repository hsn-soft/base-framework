using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.MenuDomain.Repositories;

/// <summary>A permission of a menu item, with what it is to the item.</summary>
public sealed record MenuPermissionInfo(string Code, PermissionType Type, MenuPermissionRelationType Relation, int SortOrder);

/// <summary>An item of the menu with its permissions.</summary>
public sealed record MenuWithPermissions(Menu Menu, IReadOnlyList<MenuPermissionInfo> Permissions);

public interface IMenuRepository
{
    /// <summary>The active items of a menu with the active permissions of each, parents and children together.</summary>
    Task<IReadOnlyList<MenuWithPermissions>> GetActiveAsync(MenuChannelType channel, MenuDockType dock, CancellationToken cancellationToken = default);
}
