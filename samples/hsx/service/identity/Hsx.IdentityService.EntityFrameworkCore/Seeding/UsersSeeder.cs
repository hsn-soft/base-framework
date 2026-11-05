using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>
/// One user for each role of the sample (<see cref="RolesSeeder.DefaultRoleIds"/>): in the tenant of the role, named like the role, static like the role, and linked to it, with the demo password of the sample (the production seed has no password anyone knows). A user that is there is left as it is. After the roles.</summary>
internal sealed class UsersSeeder(IdentityServiceDbContext db, ICurrentTenant currentTenant) : IDataSeeder
{
    // the sample has no notification service to send a first password, so its users have the demo password (HsxSeed.DemoPassword): not what a system of people does
    private static readonly Dictionary<string, string> s_emails = new()
    {
        ["00000000-0000-7000-8000-00000000c001"] = "system-admin@hsx.local",
        ["00000000-0000-7000-8000-00000000c002"] = "system-operator@hsx.local",
        ["00000000-0000-7000-8000-00000000c003"] = "admin@tech-summus.hsx.local",
        ["00000000-0000-7000-8000-00000000c004"] = "admin@audio-time.hsx.local",
        ["00000000-0000-7000-8000-00000000c005"] = "admin@epartner.hsx.local",
        ["00000000-0000-7000-8000-00000000c006"] = "ilbak-admin@audio-time.hsx.local",
        ["00000000-0000-7000-8000-00000000c007"] = "cnbce-admin@audio-time.hsx.local",
    };

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        List<AppRole> roles = await db.Set<AppRole>().IgnoreQueryFilters().Where(role => !role.IsDeleted && RolesSeeder.DefaultRoleIds.Contains(role.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, Tenant> tenants = await db.Set<Tenant>().IgnoreQueryFilters().ToDictionaryAsync(tenant => tenant.Id, cancellationToken).ConfigureAwait(false);
        foreach (AppRole role in roles)
        {
            string normalizedUserName = StringHelper.Normalize(role.Name);
            bool known = await db.Set<AppUser>().IgnoreQueryFilters().AnyAsync(user => user.TenantId == role.TenantId && user.NormalizedUserName == normalizedUserName, cancellationToken).ConfigureAwait(false);
            if (known)
            {
                continue;
            }

            // the tenant of the user and of its link to the role is the current one when they are saved
            using (currentTenant.Change(role.TenantId))
            {
                string email = s_emails[role.Id.ToString()];
                AppUser user = AppUser.Create(role.Name, email, role.Name, HsxSeed.DemoPassword, "en", isStatic: role.IsStatic);
                db.Set<AppUser>().Add(user);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                db.Set<AppUserRole>().Add(AppUserRole.Create(user.Id, role.Id));
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
