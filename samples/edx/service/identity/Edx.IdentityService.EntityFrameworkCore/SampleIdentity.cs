using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Edx.Shared.Helper;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Edx.IdentityService.EntityFrameworkCore;

public static class SampleIdentity
{
    /// <summary>
    /// Makes what is missing of the people of the sample, at every start: in each tenant the four roles of <see cref="EdxRoles"/> and a user of the same name for each one the <c>Seed:Users</c> section gives a password for (the passwords are the configuration's, sample values for a
    /// development database, never the code's), the user having the role of its name. A role that is made now gets its starting grants (<see cref="DefaultPermissionGrants"/>); a role and a user that exist (found by name in the tenant) are left as they are, so what an administrator changed is not
    /// undone. The ids of what is made are the ones of <see cref="EdxIdentityIds"/>. Run it after <c>EnsurePermissionsAsync</c>, which makes the permissions the grants name.
    /// </summary>
    internal static async Task EnsureSampleIdentityAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        IdentityDbContext db = services.GetRequiredService<IdentityDbContext>();
        ICurrentTenant currentTenant = services.GetRequiredService<ICurrentTenant>();
        IReadOnlyList<SeedUser> seed = services.GetRequiredService<IOptions<IdentitySeedOptions>>().Value.Users;
        Dictionary<string, Permission> permissions = await db.Permissions.ToDictionaryAsync(permission => permission.Code, cancellationToken).ConfigureAwait(false);

        foreach ((Guid tenantId, string code, _) in IdentityDatabase.s_tenants)
        {
            // everything is made as the tenant it belongs to: its rows get its id
            using (currentTenant.Change(tenantId, isSystemTenant: tenantId == EdxTenants.System))
            {
                Dictionary<string, Role> roles = await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).ToDictionaryAsync(role => role.NormalizedName, cancellationToken).ConfigureAwait(false);
                foreach (string roleName in EdxRoles.All)
                {
                    if (roles.ContainsKey(StringHelper.Normalize(roleName)))
                    {
                        continue;
                    }

                    Role role = Role.Create(EdxIdentityIds.RoleId(tenantId, roleName), roleName);
                    roles[role.NormalizedName] = role;
                    db.Roles.Add(role);
                    foreach ((string permissionCode, string? value) in DefaultPermissionGrants.For(code, roleName))
                    {
                        Permission permission = permissions[permissionCode];
                        if (permission.Type == PermissionType.Constraint)
                        {
                            db.RolePermissionConstraints.Add(RolePermissionConstraint.Grant(role, permission, value));
                        }
                        else
                        {
                            db.RolePermissions.Add(RolePermission.Grant(role, permission));
                        }
                    }
                }

                HashSet<string> users = [.. await db.Users.IgnoreQueryFilters().Where(user => user.TenantId == tenantId).Select(user => user.NormalizedUserName).ToListAsync(cancellationToken).ConfigureAwait(false)];
                foreach (SeedUser seedUser in seed.Where(user => user.Tenant.Equals(code, StringComparison.OrdinalIgnoreCase) && !users.Contains(StringHelper.Normalize(user.UserName))))
                {
                    Role role = roles[StringHelper.Normalize(seedUser.UserName)];
                    User user = User.Create(EdxIdentityIds.UserId(tenantId, seedUser.UserName), seedUser.UserName, $"{seedUser.UserName}@{code}.example", $"{seedUser.UserName} ({code})", seedUser.Password);
                    db.Users.Add(user);
                    db.UserRoles.Add(UserRole.Create(user.Id, role.Id, tenantId));
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
