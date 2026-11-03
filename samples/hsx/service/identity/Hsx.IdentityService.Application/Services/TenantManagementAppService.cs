using HsnSoft.Base;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Exceptions;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

public sealed class TenantManagementAppService(
    ITenantProvisioner provisioner,
    IRepository<Tenant, Guid> tenants,
    IRepository<AppUser, Guid> users,
    IRepository<Company, Guid> companies,
    IRepository<Customer, Guid> customers,
    IRepository<Subscription, Guid> subscriptions,
    ISubscriptionAppService subscriptionService,
    PermissionCacheSynchronizer synchronizer,
    IServiceProvider services) : ApplicationServiceBase(services), ITenantManagementAppService
{
    public async Task<ProvisionedTenantDto> CreateResellerAsync(CreateResellerDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!CurrentTenant.IsSystemTenant)
        {
            throw new BaseHttpException(403, "Only the system tenant makes a reseller.").WithKey("system_tenant_required");
        }

        string name = input.Name!.Trim().ToLowerInvariant();
        await EnsureTenantNameFreeAsync(name, cancellationToken).ConfigureAwait(false);
        AdminUserSpec? admin = await AdminAsync(input.AdminUser, cancellationToken).ConfigureAwait(false);

        ProvisionedTenant made = await provisioner.CreateResellerAsync(input.Title!.Trim(), name, admin, cancellationToken).ConfigureAwait(false);
        return await ToDtoAsync(made, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProvisionedTenantDto> CreateCompanyAsync(CreateCompanyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // a reseller makes companies for itself; the system tenant names the reseller
        Guid resellerId = CurrentTenant.IsSystemTenant
            ? input.ResellerTenantId ?? throw new BaseHttpException(400, "The system tenant names the reseller.").WithKey("reseller_required")
            : input.ResellerTenantId is { } named && named != CurrentTenant.Id
                ? throw new BaseHttpException(403, "A reseller makes companies for itself.").WithKey("reseller_not_allowed", named)
                : CurrentTenant.Id ?? throw new BaseHttpException(403, "The caller has no tenant.").WithKey("reseller_required");
        Tenant reseller = await tenants.FindAsync(resellerId, cancellationToken).ConfigureAwait(false) ?? throw TenantNotFound.For(resellerId);
        if (reseller.TenantType != TenantTypes.Reseller)
        {
            throw new BaseHttpException(403, "Only a reseller has companies.").WithKey("reseller_required");
        }

        string name = input.Name!.Trim();
        string normalized = StringHelper.Normalize(name);
        if (await companies.AnyAsync(company => company.NormalizedName == normalized, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new CompanyNameDuplicateException(name);
        }

        // the people of the company sign in to a tenant of its own, named like the company
        string tenantName = name.ToLowerInvariant();
        await EnsureTenantNameFreeAsync(tenantName, cancellationToken).ConfigureAwait(false);
        AdminUserSpec? admin = await AdminAsync(input.AdminUser, cancellationToken).ConfigureAwait(false);

        ProvisionedTenant made = await provisioner.CreateCompanyAsync(resellerId, input.Title!.Trim(), name, admin, cancellationToken).ConfigureAwait(false);
        return await ToDtoAsync(made, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // the company has to be one the caller sees (it is isolated by its tenant)
        Guid companyId = input.CompanyId.GetValueOrDefault();
        _ = await companies.FindAsync(companyId, cancellationToken).ConfigureAwait(false) ?? throw CompanyNotFound.For(companyId);

        string domain = input.Domain!.Trim();
        string normalized = StringHelper.Normalize(domain);
        if (await customers.AnyAsync(customer => customer.NormalizedDomain == normalized, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            // the same company and website is the customer that is there; any other, which the caller may not even see, is a website that is taken
            IReadOnlyList<Customer> same = await customers.GetListAsync(new ListQueryOptions<Customer> { Filter = customer => customer.NormalizedDomain == normalized && customer.CompanyId == companyId }, cancellationToken).ConfigureAwait(false);
            return same.Count > 0
                ? await customers.FindAsync<CustomerDto>(same[0].Id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(same[0].Id)
                : throw new CustomerDomainDuplicateException(domain);
        }

        Customer created = Customer.Create(companyId, domain);
        await customers.AddAsync(created, cancellationToken).ConfigureAwait(false);
        return await customers.FindAsync<CustomerDto>(created.Id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(created.Id);
    }

    public async Task<SubscriptionDto> SellSubscriptionAsync(SellSubscriptionDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid customerId = input.CustomerId.GetValueOrDefault();
        Customer customer = await customers.FindAsync(customerId, cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(customerId);
        ProductTypes product = input.ProductType.GetValueOrDefault();
        if (product == ProductTypes.Unknown)
        {
            throw new BusinessException("product_invalid", "The product is not known.").WithKey("product_invalid");
        }

        // a reseller sells as itself; the system tenant names the reseller (the subscription service checks it again)
        Guid tenantId = CurrentTenant.IsSystemTenant ? input.TenantId ?? throw new BaseHttpException(400, "The system tenant names the reseller that sells.").WithKey("subscription_tenant_required") : CurrentTenant.Id ?? throw new BaseHttpException(403, "The caller has no tenant.").WithKey("subscription_tenant_required");

        // a product sold to a customer by the same reseller is the subscription that is there
        IReadOnlyList<Subscription> there = await subscriptions.GetListAsync(
            new ListQueryOptions<Subscription> { Filter = subscription => subscription.TenantId == tenantId && subscription.CustomerId == customerId && subscription.ProductType == product }, cancellationToken).ConfigureAwait(false);
        if (there.Count > 0)
        {
            return await subscriptionService.GetAsync(there[0].Id, cancellationToken).ConfigureAwait(false);
        }

        return await subscriptionService.CreateAsync(
            new SubscriptionCreateDto { TenantId = tenantId, CompanyId = customer.CompanyId, CustomerId = customerId, ProductType = product, ValidTo = input.ValidTo }, cancellationToken).ConfigureAwait(false);
    }

    // a tenant name is taken by a deleted tenant too: the paths of the tree name it
    private async Task EnsureTenantNameFreeAsync(string name, CancellationToken cancellationToken)
    {
        string normalized = StringHelper.Normalize(name);
        if (await tenants.AnyAsync(tenant => tenant.NormalizedName == normalized, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new TenantNameDuplicateException(name);
        }
    }

    // the first administrator is checked here, before anything is written: its email is the sign-in of the whole system, and the password keeps the rules a new tenant starts with
    private async Task<AdminUserSpec?> AdminAsync(AdminUserInputDto? input, CancellationToken cancellationToken)
    {
        if (input is null)
        {
            return null;
        }

        string email = input.Email!.Trim();
        string normalizedEmail = StringHelper.Normalize(email);
        if (await users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserEmailDuplicateException(email);
        }

        IReadOnlyList<string> broken = PasswordRules.Default.Check(input.Password!);
        return broken.Count > 0
            ? throw new PasswordPolicyViolationException(broken)
            : new AdminUserSpec(input.UserName!.Trim(), email, input.DisplayName!.Trim(), input.Password!, input.LanguageCode);
    }

    private async Task<ProvisionedTenantDto> ToDtoAsync(ProvisionedTenant made, CancellationToken cancellationToken)
    {
        // the store the services read learns the administrator role of the new tenant at once
        await synchronizer.SyncRoleAsync(made.RoleId, made.TenantId, HsxRoles.Admin, cancellationToken).ConfigureAwait(false);
        return new ProvisionedTenantDto { TenantId = made.TenantId, RoleId = made.RoleId, UserId = made.UserId, CompanyId = made.CompanyId };
    }
}
