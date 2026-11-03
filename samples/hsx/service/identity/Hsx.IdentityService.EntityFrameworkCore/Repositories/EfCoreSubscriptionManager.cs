using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>
/// The sales and the links of the roles to them, in one transaction. The system tenant is the caller and works across tenants: a subscription belongs to the reseller and the roles it is given to are the reseller's, so everything here is read and written with the filter of the tenant
/// off and the tenant of a row given explicitly (the filter of the tenant is what a reseller's own calls rely on, not this).
/// </summary>
internal sealed class EfCoreSubscriptionManager(IdentityDbContext db) : ISubscriptionManager
{
    public Task<Subscription> SellAsync(Guid tenantId, Guid customerId, ProductTypes product, DateTimeOffset? validTo, string? settingsJson, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async token =>
        {
            // the whole unit runs again when the connection fails in a way that may pass: what the first try tracked is forgotten
            db.ChangeTracker.Clear();
            Customer customer = await db.Customers.FirstOrDefaultAsync(candidate => candidate.Id == customerId, token).ConfigureAwait(false) ?? throw CustomerNotFound.For(customerId);
            await EnsureResellerAsync(tenantId, token).ConfigureAwait(false);
            await EnsureNotActiveAsync(customerId, product, token).ConfigureAwait(false);

            Subscription subscription = Subscription.Create(tenantId, customer.CompanyId, customerId, product, validTo, false, settingsJson);
            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync(token).ConfigureAwait(false);
            await LinkToRolesAsync(subscription, token).ConfigureAwait(false);
            return subscription;
        }, cancellationToken);

    public Task<SubscriptionChange> EndAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async token =>
        {
            db.ChangeTracker.Clear();
            Subscription subscription = await FindActiveAsync(subscriptionId, token).ConfigureAwait(false);
            List<Guid> users = await UsersOfAsync(subscriptionId, token).ConfigureAwait(false);

            subscription.End(TimeProvider.System.GetUtcNow());
            await db.AppRoleSubscriptions.IgnoreQueryFilters().Where(link => link.SubscriptionId == subscriptionId).ExecuteDeleteAsync(token).ConfigureAwait(false);
            await db.SaveChangesAsync(token).ConfigureAwait(false);
            return new SubscriptionChange(subscription, users);
        }, cancellationToken);

    public Task<SubscriptionChange> TransferAsync(Guid subscriptionId, Guid toTenantId, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async token =>
        {
            db.ChangeTracker.Clear();
            Subscription old = await FindActiveAsync(subscriptionId, token).ConfigureAwait(false);
            if (toTenantId == old.TenantId)
            {
                throw new SubscriptionTenantInvalidException(toTenantId);
            }

            await EnsureResellerAsync(toTenantId, token).ConfigureAwait(false);
            List<Guid> users = await UsersOfAsync(subscriptionId, token).ConfigureAwait(false);

            // the old one ends first and is saved, so the new one never meets it in the unique index of the customer and the product
            old.End(TimeProvider.System.GetUtcNow());
            await db.AppRoleSubscriptions.IgnoreQueryFilters().Where(link => link.SubscriptionId == subscriptionId).ExecuteDeleteAsync(token).ConfigureAwait(false);
            await db.SaveChangesAsync(token).ConfigureAwait(false);

            // the same customer, product and settings; the scope key is the customer's and the product's, so it is the same
            Subscription next = Subscription.Create(toTenantId, old.CompanyId, old.CustomerId, old.ProductType, old.ValidTo, old.IsBlocked, old.SettingsJson);
            db.Subscriptions.Add(next);
            await db.SaveChangesAsync(token).ConfigureAwait(false);
            await LinkToRolesAsync(next, token).ConfigureAwait(false);
            return new SubscriptionChange(next, users);
        }, cancellationToken);

    public async Task LinkRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        AppRole? role = await db.AppRoles.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == roleId && !candidate.IsDeleted, cancellationToken).ConfigureAwait(false);
        if (role is null || role.RoleType == AppRoleType.System)
        {
            return;
        }

        IQueryable<Subscription> ofTheTenant = db.Subscriptions.IgnoreQueryFilters().Where(subscription => subscription.TenantId == role.TenantId && subscription.EndedAt == null && !subscription.IsDeleted);
        List<Guid> matching = role.RoleType switch
        {
            AppRoleType.Company => await ofTheTenant.Where(subscription => subscription.CompanyId == role.CompanyId).Select(subscription => subscription.Id).ToListAsync(cancellationToken).ConfigureAwait(false),
            AppRoleType.Customer => await ofTheTenant.Where(subscription => subscription.CustomerId == role.CustomerId).Select(subscription => subscription.Id).ToListAsync(cancellationToken).ConfigureAwait(false),
            _ => await ofTheTenant.Select(subscription => subscription.Id).ToListAsync(cancellationToken).ConfigureAwait(false),
        };
        HashSet<Guid> linked = [.. await db.AppRoleSubscriptions.IgnoreQueryFilters().Where(link => link.RoleId == roleId).Select(link => link.SubscriptionId).ToListAsync(cancellationToken).ConfigureAwait(false)];
        db.AppRoleSubscriptions.AddRange(matching.Where(id => !linked.Contains(id)).Select(id => AppRoleSubscription.Create(roleId, id, role.TenantId)));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // a reseller is a tenant of the type Reseller: the system tenant and the accounts do not sell
    private async Task EnsureResellerAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!await db.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.TenantType == TenantTypes.Reseller, cancellationToken).ConfigureAwait(false))
        {
            throw new SubscriptionTenantInvalidException(tenantId);
        }
    }

    // across tenants: whoever sells it, a customer has a product once
    private async Task EnsureNotActiveAsync(Guid customerId, ProductTypes product, CancellationToken cancellationToken)
    {
        if (await db.Subscriptions.IgnoreQueryFilters().AnyAsync(subscription => subscription.CustomerId == customerId && subscription.ProductType == product && subscription.EndedAt == null && !subscription.IsDeleted, cancellationToken).ConfigureAwait(false))
        {
            throw new SubscriptionAlreadyActiveException(customerId, product.ToString());
        }
    }

    private async Task<Subscription> FindActiveAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        Subscription subscription = await db.Subscriptions.IgnoreQueryFilters().FirstOrDefaultAsync(candidate => candidate.Id == subscriptionId && !candidate.IsDeleted, cancellationToken).ConfigureAwait(false)
                                    ?? throw SubscriptionNotFound.For(subscriptionId);
        return subscription.IsEnded ? throw new SubscriptionEndedException(subscriptionId) : subscription;
    }

    // the users of the roles that have the subscription: they lose it when it ends
    private Task<List<Guid>> UsersOfAsync(Guid subscriptionId, CancellationToken cancellationToken)
        => (from link in db.AppRoleSubscriptions.IgnoreQueryFilters().AsNoTracking()
            join userRole in db.AppUserRoles.IgnoreQueryFilters().AsNoTracking() on link.RoleId equals userRole.RoleId
            where link.SubscriptionId == subscriptionId
            select userRole.UserId).Distinct().ToListAsync(cancellationToken);

    // the roles of the reseller whose type covers the subscription: every Tenant role, the Company roles of its company, the Customer roles of its customer
    private async Task LinkToRolesAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        List<AppRole> roles = await db.AppRoles.IgnoreQueryFilters().AsNoTracking()
            .Where(role => !role.IsDeleted && role.TenantId == subscription.TenantId
                && (role.RoleType == AppRoleType.Tenant
                    || (role.RoleType == AppRoleType.Company && role.CompanyId == subscription.CompanyId)
                    || (role.RoleType == AppRoleType.Customer && role.CustomerId == subscription.CustomerId)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        db.AppRoleSubscriptions.AddRange(roles.Select(role => AppRoleSubscription.Create(role.Id, subscription.Id, role.TenantId)));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
