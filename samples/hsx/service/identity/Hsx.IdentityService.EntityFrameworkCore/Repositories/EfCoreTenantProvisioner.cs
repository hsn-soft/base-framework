using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.IdentityService.EntityFrameworkCore.Permissions;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>A reseller, in one transaction. A row of a tenant gets its tenant from the current tenant when it is saved, so what belongs to the new tenant is saved inside a scope of it.</summary>
internal sealed class EfCoreTenantProvisioner(IdentityDbContext db, ICurrentTenant currentTenant) : ITenantProvisioner
{
    public Task<ProvisionedTenant> CreateResellerAsync(string title, string name, AdminUserSpec? admin, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async token =>
        {
            // the whole unit runs again when the connection fails in a way that may pass: what the first try tracked is forgotten
            db.ChangeTracker.Clear();

            Tenant tenant = Tenant.Create(null, TenantTypes.Reseller, title, name);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(token).ConfigureAwait(false);

            using (currentTenant.Change(tenant.Id))
            {
                // the administrator of a reseller sees all of its sales: a Tenant role
                AppRole role = AppRole.Create(HsxRoles.Admin, AppRoleType.Tenant, isStatic: true);
                db.AppRoles.Add(role);
                await db.SaveChangesAsync(token).ConfigureAwait(false);
                db.RolePermissions.AddRange(await PermissionDatabase.StartingGrantsAsync(db, role, token).ConfigureAwait(false));

                Guid? userId = null;
                if (admin is not null)
                {
                    AppUser user = AppUser.Create(admin.UserName, admin.Email, admin.DisplayName, admin.Password, admin.LanguageCode);
                    db.AppUsers.Add(user);
                    await db.SaveChangesAsync(token).ConfigureAwait(false);
                    db.AppUserRoles.Add(AppUserRole.Create(user.Id, role.Id));
                    userId = user.Id;
                }

                await db.SaveChangesAsync(token).ConfigureAwait(false);
                return new ProvisionedTenant(tenant.Id, role.Id, userId);
            }
        }, cancellationToken);
}
