using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.MenuDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppMenuRepository(IdentityServiceDbContext db) : EfCoreRepository<IdentityServiceDbContext, AppMenu, Guid>(db), IAppMenuRepository
{
    public async Task<IReadOnlyList<MenuWithPermissions>> GetActiveAsync(MenuChannelType channel, MenuDockType dock, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AppMenu> menus = await GetListAsync(
            new ListQueryOptions<AppMenu> { Filter = menu => menu.IsActive && menu.ChannelType == channel && menu.DockType == dock },
            include: query => query.Include(menu => menu.Permissions).ThenInclude(link => link.Permission),
            cancellationToken).ConfigureAwait(false);
        return
        [
            .. menus.Select(menu => new MenuWithPermissions(
                menu,
                [
                    .. menu.Permissions
                        .Where(link => link.Permission is { IsActive: true })
                        .Select(link => new MenuPermissionInfo(link.Permission!.Code, link.Permission.Type, link.RelationType, link.SortOrder))
                        .OrderBy(info => info.SortOrder).ThenBy(info => info.Code, StringComparer.Ordinal),
                ])),
        ];
    }
}
