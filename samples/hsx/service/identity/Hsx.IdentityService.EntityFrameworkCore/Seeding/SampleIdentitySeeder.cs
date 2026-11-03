using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.IdentityService.EntityFrameworkCore.Permissions;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>
/// The tenants, the catalogue, the sales, the roles and the users of the sample (hsx/docs/TenancyModel.md). The system tenant and three resellers (tech-summus, audio-time, epartner); the company <c>ilbak</c> with its customer <c>cnbce</c>, which the system tenant defines; three sales of the
/// customer, one product each by a reseller of its own (audio-time the video platform, tech-summus the podcast, epartner the e-invoice), recorded as the system tenant records them. Each reseller has an administrator (a Tenant role, which sees all of its sales); audio-time also has a Company role
/// bound to ilbak and a Customer role bound to cnbce, each with a user: the first sees what audio-time sold to ilbak, the second what it sold to cnbce. Run again, it adds only what is missing.
/// </summary>
internal sealed class SampleIdentitySeeder(IdentityDbContext db, ICurrentTenant currentTenant, ISubscriptionManager subscriptions) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        Tenant system = await EnsureTenantAsync(HsxTenants.System, TenantTypes.System, "System", HsxTenants.SystemName, cancellationToken).ConfigureAwait(false);
        Tenant techSummus = await EnsureTenantAsync(HsxTenants.TechSummus, TenantTypes.Reseller, "Tech Summus", HsxTenants.TechSummusName, cancellationToken).ConfigureAwait(false);
        Tenant audioTime = await EnsureTenantAsync(HsxTenants.AudioTime, TenantTypes.Reseller, "Audio Time", HsxTenants.AudioTimeName, cancellationToken).ConfigureAwait(false);
        Tenant ePartner = await EnsureTenantAsync(HsxTenants.EPartner, TenantTypes.Reseller, "ePartner", HsxTenants.EPartnerName, cancellationToken).ConfigureAwait(false);

        // the catalogue: a company and a customer belong to no tenant
        if (!await db.Companies.AnyAsync(company => company.Id == HsxSeed.CompanyId, cancellationToken).ConfigureAwait(false))
        {
            Company company = Company.Create("Ilbak", HsxSeed.CompanyName);
            SetId(company, HsxSeed.CompanyId);
            db.Companies.Add(company);
        }

        if (!await db.Customers.AnyAsync(customer => customer.Id == HsxSeed.CustomerId, cancellationToken).ConfigureAwait(false))
        {
            Customer customer = Customer.Create(HsxSeed.CompanyId, HsxSeed.CustomerDomain);
            SetId(customer, HsxSeed.CustomerId);
            db.Customers.Add(customer);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // the administrator of each tenant first (a role that exists when a sale is recorded is given it), then the sales
        await EnsureUserAsync(system, "admin", "system-admin@hsx.local", "System Admin", HsxRoles.SystemAdmin, AppRoleType.System, null, null, cancellationToken).ConfigureAwait(false);
        await EnsureUserAsync(techSummus, "admin", "admin@tech-summus.hsx.local", "Tech Summus Admin", HsxRoles.Admin, AppRoleType.Tenant, null, null, cancellationToken).ConfigureAwait(false);
        await EnsureUserAsync(audioTime, "admin", "admin@audio-time.hsx.local", "Audio Time Admin", HsxRoles.Admin, AppRoleType.Tenant, null, null, cancellationToken).ConfigureAwait(false);
        await EnsureUserAsync(ePartner, "admin", "admin@epartner.hsx.local", "ePartner Admin", HsxRoles.Admin, AppRoleType.Tenant, null, null, cancellationToken).ConfigureAwait(false);

        await EnsureSaleAsync(audioTime.Id, ProductTypes.VideoPlatform, cancellationToken).ConfigureAwait(false);
        await EnsureSaleAsync(techSummus.Id, ProductTypes.Podcast, cancellationToken).ConfigureAwait(false);
        await EnsureSaleAsync(ePartner.Id, ProductTypes.EInvoice, cancellationToken).ConfigureAwait(false);

        // audio-time's people of ilbak and of cnbce: roles of the type of what they see, bound to the company and to the customer (a new role is given the sales that match it)
        await EnsureUserAsync(audioTime, "ilbak-admin", "ilbak-admin@audio-time.hsx.local", "Ilbak Admin", "ilbak-company-admin", AppRoleType.Company, HsxSeed.CompanyId, null, cancellationToken).ConfigureAwait(false);
        await EnsureUserAsync(audioTime, "cnbce-admin", "cnbce-admin@audio-time.hsx.local", "Cnbce Admin", "cnbce-customer-admin", AppRoleType.Customer, null, HsxSeed.CustomerId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Tenant> EnsureTenantAsync(Guid id, TenantTypes type, string title, string name, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            tenant = Tenant.Create(id, type, title, name);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return tenant;
    }

    // a sale is recorded as the system tenant records it (the manager checks that the customer has the product once, whoever sells it, and gives the sale to the roles of the reseller it matches)
    private async Task EnsureSaleAsync(Guid resellerId, ProductTypes product, CancellationToken cancellationToken)
    {
        if (!await db.Subscriptions.IgnoreQueryFilters().AnyAsync(subscription => subscription.CustomerId == HsxSeed.CustomerId && subscription.ProductType == product && subscription.EndedAt == null && !subscription.IsDeleted, cancellationToken).ConfigureAwait(false))
        {
            await subscriptions.SellAsync(resellerId, HsxSeed.CustomerId, product, null, null, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureUserAsync(Tenant tenant, string userName, string email, string displayName, string roleName, AppRoleType roleType, Guid? companyId, Guid? customerId, CancellationToken cancellationToken)
    {
        // rows of a tenant are made as that tenant: the persistence gives them its id
        using IDisposable scope = currentTenant.Change(tenant.Id, tenant.Id == HsxTenants.System);
        string normalizedRole = StringHelper.Normalize(roleName);
        AppRole? role = await db.AppRoles.IgnoreQueryFilters().FirstOrDefaultAsync(candidate => candidate.TenantId == tenant.Id && candidate.NormalizedName == normalizedRole, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            role = AppRole.Create(roleName, roleType, companyId, customerId, isStatic: roleType is AppRoleType.System or AppRoleType.Tenant);
            db.AppRoles.Add(role);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // a role starts with what the code says its type is granted, and with the sales its type covers
            db.RolePermissions.AddRange(await PermissionDatabase.StartingGrantsAsync(db, role, cancellationToken).ConfigureAwait(false));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await subscriptions.LinkRoleAsync(role.Id, cancellationToken).ConfigureAwait(false);
        }

        string normalizedEmail = StringHelper.Normalize(email);
        AppUser? user = await db.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            user = AppUser.Create(userName, email, displayName, HsxSeed.DemoPassword, "en");
            db.AppUsers.Add(user);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await db.AppUserRoles.IgnoreQueryFilters().AnyAsync(link => link.UserId == user.Id && link.RoleId == role.Id, cancellationToken).ConfigureAwait(false))
        {
            db.AppUserRoles.Add(AppUserRole.Create(user.Id, role.Id));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // the demo company and customer have ids that the other services know; Create makes a new one, so the id is set after
    private static void SetId<TEntity>(TEntity entity, Guid id)
        where TEntity : class
        => typeof(TEntity).GetProperty("Id")!.SetValue(entity, id);
}
