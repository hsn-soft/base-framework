using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore.Repositories;

public sealed class EfCoreMenuRepository(IdentityDbContext db) : IMenuRepository
{
    public async Task<IReadOnlyList<MenuWithPermissions>> GetActiveAsync(MenuChannelType channel, MenuDockType dock, CancellationToken cancellationToken = default)
    {
        List<Menu> menus = await db.Menus.AsNoTracking().Where(menu => menu.IsActive && menu.ChannelType == channel && menu.DockType == dock).ToListAsync(cancellationToken).ConfigureAwait(false);
        var maps = await db.MenuPermissionMaps.AsNoTracking()
            .Join(db.Permissions.Where(permission => permission.IsActive), map => map.PermissionId, permission => permission.Id, (map, permission) => new { map.MenuId, permission.Code, permission.Type, map.RelationType, map.SortOrder })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        ILookup<Guid, MenuPermissionInfo> byMenu = maps.ToLookup(map => map.MenuId, map => new MenuPermissionInfo(map.Code, map.Type, map.RelationType, map.SortOrder));
        return [.. menus.Select(menu => new MenuWithPermissions(menu, [.. byMenu[menu.Id].OrderBy(info => info.SortOrder).ThenBy(info => info.Code, StringComparer.Ordinal)]))];
    }
}
