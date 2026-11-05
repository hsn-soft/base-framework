using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Repositories;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Repositories;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Exceptions;
using Hsx.IdentityService.Domain.CustomerDomain.Repositories;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.SubscriptionDomain.Consts;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

/// <summary>A subscription that was made or changed, and the users whose roles lost a subscription by it (their sessions are ended by the caller).</summary>
public sealed record SubscriptionChange(Subscription Subscription, IReadOnlyList<Guid> UsersToSignOut);

/// <summary>
/// What is done to a sale, with the links of the roles to it, in one unit of work: either both are there or neither (the use case runs its repository calls in <see cref="IUnitOfWork.ExecuteAsync{TResult}"/>). The system tenant is the caller and it works across tenants (it sells for a reseller and the roles it updates are the reseller's).
/// The links follow the type of the role (<see cref="AppRoleType"/>): a Tenant role has every subscription of its tenant, a Company role those for its company, a Customer role those for its customer.
/// </summary>
public sealed class SubscriptionAppService(
    ISubscriptionRepository subscriptions,
    ICustomerRepository customers,
    ICompanyRepository companies,
    ITenantRepository tenants,
    IAppRoleRepository roles,
    IAppRoleSubscriptionRepository roleSubscriptions,
    IAuthRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    IServiceProvider services) : ApplicationServiceBase(services), ISubscriptionAppService
{
    public async Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await subscriptions.FindAsync<SubscriptionDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw SubscriptionNotFound.For(id);

    public async Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync(GetSubscriptionsPagedDto pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);
        PagedQueryResult<SubscriptionDto> page = await subscriptions.GetPagedListAsync<SubscriptionDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.TenantId, pagedInput.IsEnded, pagedInput.CompanyId, pagedInput.CustomerId, pagedInput.ProductType), SubscriptionSorting.Fields, SubscriptionSorting.Default),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        Subscription sold = await SellAsync(input.TenantId.GetValueOrDefault(), input.CustomerId.GetValueOrDefault(), input.ProductType.GetValueOrDefault(), input.ValidTo, input.SettingsJson, null, cancellationToken).ConfigureAwait(false);
        return await GetAsync(sold.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionDto> UpdateAsync(SubscriptionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        Subscription entity = await FindActiveAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.ValidTo, input.IsBlocked, input.SettingsJson);
        await subscriptions.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureSystemTenant();

        SubscriptionChange ended = await EndAsync(id, cancellationToken).ConfigureAwait(false);
        await refreshTokens.RevokeForUsersAsync(ended.UsersToSignOut, cancellationToken).ConfigureAwait(false);
        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionDto> TransferAsync(SubscriptionTransferDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        // the users of the roles that had the old subscription lose it: their sessions end (the access token they hold ends by itself)
        SubscriptionChange change = await TransferAsync(input.Id.GetValueOrDefault(), input.TenantId.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        await refreshTokens.RevokeForUsersAsync(change.UsersToSignOut, cancellationToken).ConfigureAwait(false);
        return await GetAsync(change.Subscription.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default)
    {
        // the subscriptions the caller sees (the filter of the tenant); the catalogue has no filter, so the companies and the customers are read by the ids that are in them
        IReadOnlyList<Subscription> seen = await subscriptions.GetListAsync(new ListQueryOptions<Subscription>(), cancellationToken).ConfigureAwait(false);
        Guid[] companyIds = [.. seen.Select(subscription => subscription.CompanyId).Distinct()];
        Guid[] customerIds = [.. seen.Select(subscription => subscription.CustomerId).Distinct()];
        IReadOnlyList<Company> foundCompanies = await companies.GetListAsync(new ListQueryOptions<Company> { Filter = company => companyIds.Contains(company.Id) }, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Customer> foundCustomers = await customers.GetListAsync(new ListQueryOptions<Customer> { Filter = customer => customerIds.Contains(customer.Id) }, cancellationToken).ConfigureAwait(false);

        return new SubscriptionFiltersDto
        {
            Companies = [.. foundCompanies.OrderBy(company => company.Title, StringComparer.Ordinal).Select(company => new FilterCompanyDto { Id = company.Id, Title = company.Title })],
            Customers = [.. foundCustomers.OrderBy(customer => customer.Domain, StringComparer.Ordinal).Select(customer => new FilterCustomerDto { Id = customer.Id, CompanyId = customer.CompanyId, Domain = customer.Domain })],
            Products = [.. seen.Select(subscription => subscription.ProductType).Distinct().Order()],
        };
    }

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Subscription, bool>> Filter(Guid? tenantId, bool? isEnded, Guid? companyId, Guid? customerId, ProductTypes? productType)
    {
        return new FilterBuilder<Subscription>()
            .And(tenantId is null ? null : entity => entity.TenantId == tenantId.Value)
            .And(isEnded is null ? null : entity => (entity.EndedAt != null) == isEnded.Value)
            .And(companyId is null ? null : entity => entity.CompanyId == companyId.Value)
            .And(customerId is null ? null : entity => entity.CustomerId == customerId.Value)
            .And(productType is null ? null : entity => entity.ProductType == productType.Value)
            .Build();
    }

    /// <summary>Records that a reseller sells a product to a customer (the company is the customer's), and gives the subscription to the roles of the reseller that it matches. <paramref name="id"/> is given by the seed, which sells under fixed ids.</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such customer.</exception>
    /// <exception cref="SubscriptionTenantInvalidException">The tenant is not a reseller.</exception>
    /// <exception cref="SubscriptionAlreadyActiveException">The customer has the product already.</exception>
    public Task<Subscription> SellAsync(Guid tenantId, Guid customerId, ProductTypes product, DateTimeOffset? validTo, string? settingsJson, Guid? id = null, CancellationToken cancellationToken = default)
        => unitOfWork.ExecuteAsync(async token =>
        {
            Customer customer = await customers.FindAsync(customerId, token).ConfigureAwait(false) ?? throw CustomerNotFound.For(customerId);
            await EnsureResellerAsync(tenantId, token).ConfigureAwait(false);
            await EnsureNotActiveAsync(customerId, product, token).ConfigureAwait(false);

            Subscription subscription = Subscription.Create(tenantId, customer.CompanyId, customerId, product, validTo, false, settingsJson, id);
            await subscriptions.AddAsync(subscription, token).ConfigureAwait(false);
            await LinkToRolesAsync(subscription, token).ConfigureAwait(false);
            return subscription;
        }, cancellationToken);

    /// <summary>Ends a sale: it gets its end and is taken from every role.</summary>
    /// <exception cref="SubscriptionEndedException">It has ended already.</exception>
    public Task<SubscriptionChange> EndAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        => unitOfWork.ExecuteAsync(async token =>
        {
            Subscription subscription = await FindActiveAsync(subscriptionId, token).ConfigureAwait(false);
            IReadOnlyList<Guid> users = await roleSubscriptions.GetUserIdsOfSubscriptionAsync(subscriptionId, token).ConfigureAwait(false);

            subscription.End(time.GetUtcNow());
            await subscriptions.UpdateAsync(subscription, token).ConfigureAwait(false);
            await roleSubscriptions.DeleteForSubscriptionAsync(subscriptionId, token).ConfigureAwait(false);
            return new SubscriptionChange(subscription, users);
        }, cancellationToken);

    /// <summary>Passes a sale to another reseller: the old subscription ends and is taken from every role, a new one starts now for the other reseller (the same customer, product and settings; the scope key does not change) and is given to the roles of that reseller it matches.</summary>
    /// <exception cref="SubscriptionEndedException">It has ended already.</exception>
    /// <exception cref="SubscriptionTenantInvalidException">The other tenant is not a reseller, or it is the one that sells it.</exception>
    public Task<SubscriptionChange> TransferAsync(Guid subscriptionId, Guid toTenantId, CancellationToken cancellationToken = default)
        => unitOfWork.ExecuteAsync(async token =>
        {
            Subscription old = await FindActiveAsync(subscriptionId, token).ConfigureAwait(false);
            if (toTenantId == old.TenantId)
            {
                throw new SubscriptionTenantInvalidException(toTenantId);
            }

            await EnsureResellerAsync(toTenantId, token).ConfigureAwait(false);
            IReadOnlyList<Guid> users = await roleSubscriptions.GetUserIdsOfSubscriptionAsync(subscriptionId, token).ConfigureAwait(false);

            // the old one ends first and is saved, so the new one never meets it in the unique index of the customer and the product
            old.End(time.GetUtcNow());
            await subscriptions.UpdateAsync(old, token).ConfigureAwait(false);
            await roleSubscriptions.DeleteForSubscriptionAsync(subscriptionId, token).ConfigureAwait(false);

            Subscription next = Subscription.Create(toTenantId, old.CompanyId, old.CustomerId, old.ProductType, old.ValidTo, old.IsBlocked, old.SettingsJson);
            await subscriptions.AddAsync(next, token).ConfigureAwait(false);
            await LinkToRolesAsync(next, token).ConfigureAwait(false);
            return new SubscriptionChange(next, users);
        }, cancellationToken);

    /// <summary>Gives a role the subscriptions of its tenant that its type matches (a new role): the ones that are not ended. A System role has none.</summary>
    public async Task LinkRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        AppRole? role = await roles.FindOfAnyTenantAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (role is null || role.RoleType == AppRoleType.System)
        {
            return;
        }

        IReadOnlyList<Guid> matching = await subscriptions.GetActiveIdsOfTenantAsync(
            role.TenantId,
            role.RoleType == AppRoleType.Company ? role.CompanyId : null,
            role.RoleType == AppRoleType.Customer ? role.CustomerId : null,
            cancellationToken).ConfigureAwait(false);
        HashSet<Guid> linked = [.. await roleSubscriptions.GetSubscriptionIdsOfRoleAsync(roleId, cancellationToken).ConfigureAwait(false)];
        await roleSubscriptions.AddManyAsync(matching.Where(id => !linked.Contains(id)).Select(id => AppRoleSubscription.Create(roleId, id, role.TenantId)), cancellationToken).ConfigureAwait(false);
    }

    // a reseller is a tenant of the type Reseller: the system tenant does not sell
    private async Task EnsureResellerAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!await tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.TenantType == TenantTypes.Reseller, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new SubscriptionTenantInvalidException(tenantId);
        }
    }

    private async Task EnsureNotActiveAsync(Guid customerId, ProductTypes product, CancellationToken cancellationToken)
    {
        if (await subscriptions.HasActiveAsync(customerId, product, cancellationToken).ConfigureAwait(false))
        {
            throw new SubscriptionAlreadyActiveException(customerId, product.ToString());
        }
    }

    private async Task<Subscription> FindActiveAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        Subscription subscription = await subscriptions.FindOfAnyTenantAsync(subscriptionId, cancellationToken).ConfigureAwait(false) ?? throw SubscriptionNotFound.For(subscriptionId);
        return subscription.IsEnded ? throw new SubscriptionEndedException(subscriptionId) : subscription;
    }

    // the roles of the reseller whose type covers the subscription: every Tenant role, the Company roles of its company, the Customer roles of its customer
    private async Task LinkToRolesAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppRole> covering = await roles.GetCoveringAsync(subscription, cancellationToken).ConfigureAwait(false);
        await roleSubscriptions.AddManyAsync(covering.Select(role => AppRoleSubscription.Create(role.Id, subscription.Id, role.TenantId)), cancellationToken).ConfigureAwait(false);
    }
}
