using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// The sales. A subscription belongs to the reseller that sells it (the filter of the tenant: a reseller sees its own and the ones of the tenants under it), but only the system tenant records, changes, ends and passes them on; the manager does it with the roles' links in one transaction.
/// </summary>
public sealed class SubscriptionAppService(
    IRepository<Subscription, Guid> repository,
    IRepository<Customer, Guid> customers,
    IRepository<Company, Guid> companies,
    ISubscriptionManager manager,
    IRefreshTokenStore refreshTokens,
    IServiceProvider services) : ApplicationServiceBase(services), ISubscriptionAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<SubscriptionDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw SubscriptionNotFound.For(id);

    public async Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync(GetSubscriptionsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<SubscriptionDto> page = await repository.GetPagedListAsync<SubscriptionDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.TenantId, pagedInput.IsEnded, pagedInput.CompanyId, pagedInput.CustomerId, pagedInput.ProductType), SubscriptionSorting.Fields, SubscriptionSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        Subscription sold = await manager.SellAsync(input.TenantId.GetValueOrDefault(), input.CustomerId.GetValueOrDefault(), input.ProductType.GetValueOrDefault(), input.ValidTo, input.SettingsJson, cancellationToken).ConfigureAwait(false);
        return await GetAsync(sold.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SubscriptionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        Subscription entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        if (entity.IsEnded)
        {
            throw new SubscriptionEndedException(entity.Id);
        }

        entity.Update(input.ValidTo, input.IsBlocked, input.SettingsJson);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureSystemTenant();

        SubscriptionChange ended = await manager.EndAsync(id, cancellationToken).ConfigureAwait(false);
        await refreshTokens.RevokeForUsersAsync(ended.UsersToSignOut, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionDto> TransferAsync(SubscriptionTransferDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        // the users of the roles that had the old subscription lose it: their sessions end (the access token they hold ends by itself)
        SubscriptionChange change = await manager.TransferAsync(input.Id.GetValueOrDefault(), input.TenantId.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        await refreshTokens.RevokeForUsersAsync(change.UsersToSignOut, cancellationToken).ConfigureAwait(false);
        return await GetAsync(change.Subscription.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default)
    {
        // the subscriptions the caller sees (the filter of the tenant); the catalogue has no filter, so the companies and the customers are read by the ids that are in them
        IReadOnlyList<Subscription> seen = await repository.GetListAsync(new ListQueryOptions<Subscription>(), cancellationToken).ConfigureAwait(false);
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

    private async Task<Subscription> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw SubscriptionNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Subscription, bool>> Filter(string? searchText, Guid? tenantId, bool? isEnded, Guid? companyId, Guid? customerId, ProductTypes? productType)
    {
        string? search = Normalized(searchText);
        return new FilterBuilder<Subscription>()
            .And(search is null ? null : entity => entity.ScopeKey.Contains(search))
            .And(tenantId is null ? null : entity => entity.TenantId == tenantId.Value)
            .And(isEnded is null ? null : entity => (entity.EndedAt != null) == isEnded.Value)
            .And(companyId is null ? null : entity => entity.CompanyId == companyId.Value)
            .And(customerId is null ? null : entity => entity.CustomerId == customerId.Value)
            .And(productType is null ? null : entity => entity.ProductType == productType.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
