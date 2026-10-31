using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.IdentityService.EntityFrameworkCore;

public static class PermissionDatabase
{
    /// <summary>The codes of every permission of the system: the constants of <c>Edx.Shared.Helper</c>, which are the only list there is.</summary>
    public static IReadOnlyList<string> CatalogCodes { get; } =
    [
        .. PermissionCatalog.CodesOf(typeof(EdxPagePermissions)),
        .. PermissionCatalog.CodesOf(typeof(EdxEndpointPermissions)),
        .. PermissionCatalog.CodesOf(typeof(EdxOperationPermissions)),
        .. PermissionCatalog.CodesOf(typeof(EdxConstraintPermissions)),
    ];

    /// <summary>
    /// Keeps the tables of the permissions and of the menu in step with the code at every start. A permission that is new is added, one that is not in the code any more is made inactive (nothing is deleted: grants name it), one that came back is active again. A permission that is
    /// added now is granted to the sample roles that <see cref="DefaultPermissionGrants"/> names it for; one that was added before is never granted again, so what a tenant's administrator took away stays taken away. Then the menu: the items of <see cref="MenuCatalog"/> are
    /// added or brought up to date with their permissions (<c>MenuPermissionMap</c>), and an item that is not in the code any more is made inactive. Run it after <c>EnsureIdentityDatabaseAsync</c>, which makes the roles.
    /// </summary>
    internal static async Task EnsurePermissionsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        IdentityDbContext db = services.GetRequiredService<IdentityDbContext>();
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
        await EnsureMenusAsync(db, services.GetRequiredService<TimeProvider>().GetUtcNow(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task GrantAddedAsync(IdentityDbContext db, List<Permission> added, CancellationToken cancellationToken)
    {
        if (added.Count == 0)
        {
            return;
        }

        Dictionary<string, Permission> addedByCode = added.ToDictionary(permission => permission.Code);
        var roles = await db.Roles.IgnoreQueryFilters().Join(db.Tenants, role => role.TenantId, tenant => tenant.Id, (role, tenant) => new { Role = role, TenantCode = tenant.Code }).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var item in roles)
        {
            foreach ((string code, string? value) in DefaultPermissionGrants.For(item.TenantCode, item.Role.Name).Where(grant => addedByCode.ContainsKey(grant.Code)))
            {
                Permission permission = addedByCode[code];
                if (permission.Type == PermissionType.Constraint)
                {
                    db.RolePermissionConstraints.Add(RolePermissionConstraint.Grant(item.Role, permission, value));
                }
                else
                {
                    db.RolePermissions.Add(RolePermission.Grant(item.Role, permission));
                }
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
