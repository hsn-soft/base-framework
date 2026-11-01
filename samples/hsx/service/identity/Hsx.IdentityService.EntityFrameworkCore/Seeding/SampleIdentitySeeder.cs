using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>
/// The tenants, the demo company, customer and subscriptions, the roles and the users of the sample. Techsummus (a reseller, Acme under it) sold the demo customer the video platform; Pixel (another reseller) sold it the vertical video. Each
/// reseller has an administrator whose role holds the subscription of its own sale, so the same customer shows different content to each of them. Run again, it adds only what is missing.
/// </summary>
internal sealed class SampleIdentitySeeder(IdentityDbContext db, ICurrentTenant currentTenant) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // the seeder writes for every tenant, so it works as the system tenant (the tenant of a row is given from the current tenant when it is added)
        Tenant system = await EnsureTenantAsync(HsxTenants.System, TenantTypes.System, "System", HsxTenants.SystemName, null, cancellationToken).ConfigureAwait(false);
        Tenant techsummus = await EnsureTenantAsync(HsxTenants.Techsummus, TenantTypes.Reseller, "Techsummus", HsxTenants.TechsummusName, null, cancellationToken).ConfigureAwait(false);
        Tenant pixel = await EnsureTenantAsync(HsxTenants.Pixel, TenantTypes.Reseller, "Pixel", HsxTenants.PixelName, null, cancellationToken).ConfigureAwait(false);
        Tenant acme = await EnsureTenantAsync(HsxTenants.Acme, TenantTypes.Account, "Acme", HsxTenants.AcmeName, techsummus, cancellationToken).ConfigureAwait(false);

        if (!await db.Companies.AnyAsync(company => company.Id == HsxSeed.CompanyId, cancellationToken).ConfigureAwait(false))
        {
            Company company = Company.Create("Demo Company", HsxSeed.CompanyName);
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

        Subscription techsummusSale = await EnsureSubscriptionAsync(techsummus.Id, ProductTypes.VideoPlatform, cancellationToken).ConfigureAwait(false);
        Subscription pixelSale = await EnsureSubscriptionAsync(pixel.Id, ProductTypes.VerticalVideo, cancellationToken).ConfigureAwait(false);

        await EnsureUserAsync(system, "admin", "system-admin@hsx.local", "System Admin", "SystemAdmin", null, cancellationToken).ConfigureAwait(false);
        await EnsureUserAsync(techsummus, "admin", "admin@techsummus.hsx.local", "Techsummus Admin", "Admin", techsummusSale, cancellationToken).ConfigureAwait(false);
        await EnsureUserAsync(pixel, "admin", "admin@pixel.hsx.local", "Pixel Admin", "Admin", pixelSale, cancellationToken).ConfigureAwait(false);
        await EnsureUserAsync(acme, "admin", "admin@acme.hsx.local", "Acme Admin", "Admin", null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Tenant> EnsureTenantAsync(Guid id, TenantTypes type, string title, string name, Tenant? parent, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            tenant = Tenant.Create(id, type, title, name, parent);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return tenant;
    }

    private async Task<Subscription> EnsureSubscriptionAsync(Guid resellerId, ProductTypes product, CancellationToken cancellationToken)
    {
        Subscription? subscription = await db.Subscriptions.FirstOrDefaultAsync(candidate => candidate.ResellerTenantId == resellerId && candidate.CustomerId == HsxSeed.CustomerId && candidate.ProductType == product, cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            subscription = Subscription.Create(resellerId, HsxSeed.CompanyId, HsxSeed.CustomerId, product, null, false, null);
            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return subscription;
    }

    private async Task EnsureUserAsync(Tenant tenant, string userName, string email, string displayName, string roleName, Subscription? subscription, CancellationToken cancellationToken)
    {
        // rows of a tenant are made as that tenant: the persistence gives them its id
        using IDisposable scope = currentTenant.Change(tenant.Id, tenant.Id == HsxTenants.System);
        string normalizedRole = HsnSoft.Base.Text.StringHelper.Normalize(roleName);
        AppRole? role = await db.AppRoles.IgnoreQueryFilters().FirstOrDefaultAsync(candidate => candidate.TenantId == tenant.Id && candidate.NormalizedName == normalizedRole, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            role = AppRole.Create(roleName, isStatic: true);
            db.AppRoles.Add(role);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (subscription is not null && !await db.AppRoleSubscriptions.IgnoreQueryFilters().AnyAsync(link => link.RoleId == role.Id && link.SubscriptionId == subscription.Id, cancellationToken).ConfigureAwait(false))
        {
            db.AppRoleSubscriptions.Add(AppRoleSubscription.Create(role.Id, subscription.Id));
        }

        string normalizedEmail = HsnSoft.Base.Text.StringHelper.Normalize(email);
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
