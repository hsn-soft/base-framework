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
        .. PermissionCatalog.CodesOf(typeof(EdxEndpointPermissions)),
        .. PermissionCatalog.CodesOf(typeof(EdxOperationPermissions)),
        .. PermissionCatalog.CodesOf(typeof(EdxConstraintPermissions)),
    ];

    /// <summary>
    /// Keeps the table of the permissions in step with the code at every start: a code that is new is added, one that is not in the code any more is made inactive (nothing is deleted: grants name it), one that came back is active again. The first time
    /// (when nothing has been granted to any role) the roles of the sample tenants get their starting grants (<see cref="DefaultPermissionGrants"/>). Run it after <c>EnsureIdentityDatabaseAsync</c>, which makes the roles.
    /// </summary>
    public static async Task EnsurePermissionsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            List<Permission> known = await db.Permissions.ToListAsync(cancellationToken).ConfigureAwait(false);
            HashSet<string> codes = [.. CatalogCodes];
            db.Permissions.AddRange(codes.Except(known.Select(permission => permission.Code)).Select(code => Permission.Create(code)));
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

            if (await db.RolePermissions.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            Dictionary<string, Permission> byCode = await db.Permissions.ToDictionaryAsync(permission => permission.Code, cancellationToken).ConfigureAwait(false);
            var roles = await db.Roles.IgnoreQueryFilters().Join(db.Tenants, role => role.TenantId, tenant => tenant.Id, (role, tenant) => new { Role = role, TenantCode = tenant.Code }).ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in roles)
            {
                db.RolePermissions.AddRange(DefaultPermissionGrants.For(item.TenantCode, item.Role.Name).Select(grant => RolePermission.Grant(item.Role, byCode[grant.Code], grant.Value)));
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
