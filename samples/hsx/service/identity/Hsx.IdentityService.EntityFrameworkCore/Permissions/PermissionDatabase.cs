using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.MenuDomain.Catalog;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Catalog;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Permissions;

public static class PermissionDatabase
{
    /// <summary>
    /// Keeps the tables of the permissions and of the menu in step with the code at every start. A permission that is new is added, one that is not in the code any more is made inactive (nothing is deleted: grants name it), one that came back is active again.
    /// What is added now is granted to the roles whose type the catalog names it for (a constraint with its starting value); one that was added before is never granted again, so what the administrator of a tenant took away stays
    /// taken away. Then the menu: the items of the catalog are added or brought up to date with their permissions, and an item that is not in the code any more is made inactive. Run it before the roles are made, which are granted their starting grants then.
    /// </summary>
    internal static async Task EnsurePermissionsAsync(IdentityServiceDbContext db, IPermissionCatalog catalog, TimeProvider time, CancellationToken cancellationToken)
    {
        List<Permission> known = await db.Set<Permission>().ToListAsync(cancellationToken).ConfigureAwait(false);
        HashSet<string> codes = [.. catalog.Codes];
        List<Permission> added = [.. codes.Except(known.Select(permission => permission.Code)).Select(code => Permission.Create(code))];
        db.Set<Permission>().AddRange(added);
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
        await GrantAddedAsync(db, catalog, added, cancellationToken).ConfigureAwait(false);
        await EnsureMenusAsync(db, catalog, time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>What a role starts with, as rows to add: the permissions the catalog names for its type, and its constraints with their starting values, found in the catalogue.</summary>
    internal static async Task<StartingGrants> StartingGrantsAsync(IdentityServiceDbContext db, IPermissionCatalog catalog, AppRole role, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> plainCodes = catalog.GrantsFor(role.RoleType, role.Name);
        IReadOnlyDictionary<string, string> limits = catalog.ConstraintsFor(role.RoleType, role.Name);
        List<Permission> permissions = await db.Set<Permission>().Where(permission => permission.IsActive && (plainCodes.Contains(permission.Code) || limits.Keys.Contains(permission.Code))).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new(
            [.. permissions.Where(permission => permission.Type != PermissionType.Constraint).Select(permission => AppRolePermission.Grant(role, permission))],
            [.. permissions.Where(permission => permission.Type == PermissionType.Constraint).Select(permission => AppRolePermissionConstraint.Grant(role, permission, limits[permission.Code]))]);
    }

    private static async Task GrantAddedAsync(IdentityServiceDbContext db, IPermissionCatalog catalog, List<Permission> added, CancellationToken cancellationToken)
    {
        if (added.Count == 0)
        {
            return;
        }

        Dictionary<string, Permission> addedByCode = added.ToDictionary(permission => permission.Code);
        List<AppRole> roles = await db.Set<AppRole>().IgnoreQueryFilters().ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (AppRole role in roles)
        {
            foreach (string code in catalog.GrantsFor(role.RoleType, role.Name).Where(addedByCode.ContainsKey))
            {
                db.Set<AppRolePermission>().Add(AppRolePermission.Grant(role, addedByCode[code]));
            }

            foreach ((string code, string value) in catalog.ConstraintsFor(role.RoleType, role.Name).Where(limit => addedByCode.ContainsKey(limit.Key)))
            {
                db.Set<AppRolePermissionConstraint>().Add(AppRolePermissionConstraint.Grant(role, addedByCode[code], value));
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureMenusAsync(IdentityServiceDbContext db, IPermissionCatalog catalog, DateTimeOffset now, CancellationToken cancellationToken)
    {
        Dictionary<string, AppMenu> menus = await db.Set<AppMenu>().ToDictionaryAsync(menu => menu.Code, cancellationToken).ConfigureAwait(false);
        Dictionary<string, Permission> permissions = await db.Set<Permission>().ToDictionaryAsync(permission => permission.Code, cancellationToken).ConfigureAwait(false);
        HashSet<string> defined = [.. catalog.BackOfficeMenu.Select(definition => definition.Code)];

        // a leaf with no visibility permission would be shown to nobody: the code is wrong, and the service does not start
        foreach (MenuDefinition leaf in catalog.BackOfficeMenu.Where(definition => definition.NodeType == MenuNodeType.Leaf && !definition.Permissions.Any(map => map.Relation == MenuPermissionRelationType.Visibility)))
        {
            throw new InvalidOperationException($"The menu item '{leaf.Code}' is a leaf and has no visibility permission.");
        }

        // the parents come before their children in the catalogue
        foreach (MenuDefinition definition in catalog.BackOfficeMenu)
        {
            if (!menus.TryGetValue(definition.Code, out AppMenu? menu))
            {
                menu = AppMenu.Create(definition.Code, MenuChannelType.BackOffice, MenuDockType.SidebarMenu, definition.NodeType, null, now);
                menus[definition.Code] = menu;
                db.Set<AppMenu>().Add(menu);
            }

            menu.Define(definition.Title, definition.Route, definition.Icon, definition.SortOrder, definition.Parent is null ? null : menus[definition.Parent].Id, isActive: true);
        }

        foreach (AppMenu gone in menus.Values.Where(menu => !defined.Contains(menu.Code)))
        {
            gone.Define(gone.Title, gone.Route, gone.Icon, gone.SortOrder, gone.ParentId, isActive: false);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // the permissions of an item are set again each time: the code says what they are
        List<Guid> menuIds = [.. catalog.BackOfficeMenu.Select(definition => menus[definition.Code].Id)];
        await db.Set<AppMenuPermission>().Where(map => menuIds.Contains(map.MenuId)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        foreach (MenuDefinition definition in catalog.BackOfficeMenu)
        {
            int order = 0;
            foreach (MenuPermissionDefinition map in definition.Permissions)
            {
                db.Set<AppMenuPermission>().Add(AppMenuPermission.Create(menus[definition.Code].Id, permissions[map.Permission].Id, map.Relation, order += 10));
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>What a role starts with: the grants that carry no value and the constraints with their values.</summary>
internal sealed record StartingGrants(List<AppRolePermission> Permissions, List<AppRolePermissionConstraint> Constraints);
