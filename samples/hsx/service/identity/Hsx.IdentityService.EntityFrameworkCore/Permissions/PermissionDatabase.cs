using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Permissions;

public static class PermissionDatabase
{
    /// <summary>The codes of every permission of the system: the constants of <c>Hsx.Shared.Helper</c>, which are the only list there is.</summary>
    public static IReadOnlyList<string> CatalogCodes { get; } =
    [
        .. PermissionCatalog.CodesOf(typeof(HsxPagePermissions)),
        .. PermissionCatalog.CodesOf(typeof(HsxEndpointPermissions)),
    ];

    /// <summary>
    /// Keeps the tables of the permissions and of the menu in step with the code at every start. A permission that is new is added, one that is not in the code any more is made inactive (nothing is deleted: grants name it), one that came back is active again.
    /// What is added now is granted to the roles whose type <see cref="DefaultPermissionGrants"/> names it for; one that was added before is never granted again, so what the administrator of a tenant took away stays taken away. Then the menu: the items of
    /// <see cref="MenuCatalog"/> are added or brought up to date with their permissions (<c>MenuPermissionMap</c>), and an item that is not in the code any more is made inactive. Run it before the roles are made, which are granted their starting grants then.
    /// </summary>
    internal static async Task EnsurePermissionsAsync(IdentityDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        List<Permission> known = await db.Permissions.ToListAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> codes = [.. CatalogCodes];
        List<Permission> added = [.. codes.Except(known.Select(permission => permission.Code)).Select(code => Permission.Create(code))];
        db.Permissions.AddRange(added);
        foreach (Permission permission in known)
        {
            if (codes.Contains(permission.Code))
            {
                permission.Activate();
            }
            else
            {
                permission.Deactivate();
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await GrantAddedAsync(db, added, cancellationToken).ConfigureAwait(false);
        await EnsureMenusAsync(db, time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The permissions a role starts with, as rows to add: what <see cref="DefaultPermissionGrants"/> names for its type, found in the catalogue.</summary>
    internal static async Task<List<RolePermission>> StartingGrantsAsync(IdentityDbContext db, AppRole role, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> codes = DefaultPermissionGrants.ForRole(role.RoleType, role.Name);
        List<Permission> permissions = await db.Permissions.Where(permission => permission.IsActive && codes.Contains(permission.Code)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. permissions.Select(permission => RolePermission.Grant(role, permission))];
    }

    private static async Task GrantAddedAsync(IdentityDbContext db, List<Permission> added, CancellationToken cancellationToken)
    {
        if (added.Count == 0)
        {
            return;
        }

        Dictionary<string, Permission> addedByCode = added.ToDictionary(permission => permission.Code);
        List<AppRole> roles = await db.AppRoles.IgnoreQueryFilters().ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (AppRole role in roles)
        {
            foreach (string code in DefaultPermissionGrants.ForRole(role.RoleType, role.Name).Where(addedByCode.ContainsKey))
            {
                db.RolePermissions.Add(RolePermission.Grant(role, addedByCode[code]));
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureMenusAsync(IdentityDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        Dictionary<string, Menu> menus = await db.Menus.ToDictionaryAsync(menu => menu.Code, cancellationToken).ConfigureAwait(false);
        Dictionary<string, Permission> permissions = await db.Permissions.ToDictionaryAsync(permission => permission.Code, cancellationToken).ConfigureAwait(false);
        HashSet<string> defined = [.. MenuCatalog.BackOffice.Select(definition => definition.Code)];

        // a leaf with no visibility permission would be shown to nobody: the code is wrong, and the service does not start
        foreach (MenuDefinition leaf in MenuCatalog.BackOffice.Where(definition => definition.NodeType == MenuNodeType.Leaf && !definition.Permissions.Any(map => map.Relation == MenuPermissionRelationType.Visibility)))
        {
            throw new InvalidOperationException($"The menu item '{leaf.Code}' is a leaf and has no visibility permission.");
        }

        // the parents come before their children in the catalogue
        foreach (MenuDefinition definition in MenuCatalog.BackOffice)
        {
            if (!menus.TryGetValue(definition.Code, out Menu? menu))
            {
                menu = Menu.Create(definition.Code, MenuChannelType.BackOffice, MenuDockType.SidebarMenu, definition.NodeType, null, now);
                menus[definition.Code] = menu;
                db.Menus.Add(menu);
            }

            menu.Define(definition.Title, definition.Route, definition.Icon, definition.SortOrder, definition.Parent is null ? null : menus[definition.Parent].Id, isActive: true);
        }

        foreach (Menu gone in menus.Values.Where(menu => !defined.Contains(menu.Code)))
        {
            gone.Define(gone.Title, gone.Route, gone.Icon, gone.SortOrder, gone.ParentId, isActive: false);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // the permissions of an item are set again each time: the code says what they are
        List<Guid> menuIds = [.. MenuCatalog.BackOffice.Select(definition => menus[definition.Code].Id)];
        await db.MenuPermissionMaps.Where(map => menuIds.Contains(map.MenuId)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        foreach (MenuDefinition definition in MenuCatalog.BackOffice)
        {
            int order = 0;
            foreach (MenuPermissionDefinition map in definition.Permissions)
            {
                db.MenuPermissionMaps.Add(MenuPermissionMap.Create(menus[definition.Code].Id, permissions[map.Permission].Id, map.Relation, order += 10));
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
