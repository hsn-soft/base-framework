using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.IdentityService.EntityFrameworkCore.Permissions;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>The tenants, in one transaction. A row of a tenant gets its tenant from the current tenant when it is saved, so what belongs to the new tenant is saved inside a scope of it, and what belongs to the reseller inside a scope of the reseller.</summary>
internal sealed class EfCoreTenantProvisioner(IdentityDbContext db, ICurrentTenant currentTenant) : ITenantProvisioner
{
    public Task<ProvisionedTenant> CreateResellerAsync(string title, string name, AdminUserSpec? admin, CancellationToken cancellationToken = default)
        => ProvisionAsync(null, TenantTypes.Reseller, title, name, admin, withCompany: false, cancellationToken);

    public Task<ProvisionedTenant> CreateCompanyAsync(Guid resellerTenantId, string title, string name, AdminUserSpec? admin, CancellationToken cancellationToken = default)
        => ProvisionAsync(resellerTenantId, TenantTypes.Account, title, name, admin, withCompany: true, cancellationToken);

    private Task<ProvisionedTenant> ProvisionAsync(Guid? parentId, TenantTypes type, string title, string name, AdminUserSpec? admin, bool withCompany, CancellationToken cancellationToken)
        => db.ExecuteInTransactionAsync(async token =>
        {
            // the whole unit runs again when the connection fails in a way that may pass: what the first try tracked is forgotten
            db.ChangeTracker.Clear();

            Tenant? parent = parentId is { } id ? await db.Tenants.FirstOrDefaultAsync(candidate => candidate.Id == id, token).ConfigureAwait(false) ?? throw TenantNotFound.For(id) : null;
            Tenant tenant = Tenant.Create(null, type, title, name, parent);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(token).ConfigureAwait(false);

            Guid? companyId = null;
            if (withCompany)
            {
                // the company is the reseller's, and keeps the link to the tenant of its people
                using (currentTenant.Change(parent!.Id))
                {
                    Company company = Company.Create(title, name);
                    company.LinkAccountTenant(tenant.Id);
                    db.Companies.Add(company);
                    await db.SaveChangesAsync(token).ConfigureAwait(false);
                    companyId = company.Id;
                }
            }

            using (currentTenant.Change(tenant.Id))
            {
                AppRole role = AppRole.Create(HsxRoles.Admin, isStatic: true);
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
                return new ProvisionedTenant(tenant.Id, role.Id, userId, companyId);
            }
        }, cancellationToken);
}
