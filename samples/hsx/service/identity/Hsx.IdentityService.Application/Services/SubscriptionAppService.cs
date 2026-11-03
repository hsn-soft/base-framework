using System.Linq.Expressions;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

public sealed class SubscriptionAppService(IRepository<Subscription, Guid> repository, IRepository<Customer, Guid> customers, IServiceProvider services) : ApplicationServiceBase(services), ISubscriptionAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<SubscriptionDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw SubscriptionNotFound.For(id);

    public async Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync(GetSubscriptionsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<SubscriptionDto> page = await repository.GetPagedListAsync<SubscriptionDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.TenantId, pagedInput.CompanyId, pagedInput.CustomerId, pagedInput.ProductType), SubscriptionSorting.Fields, SubscriptionSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // the customer has to be one the caller sees (it is isolated by its tenant), and the subscription is of the company the customer is of
        Guid customerId = input.CustomerId.GetValueOrDefault();
        Customer customer = await customers.FindAsync(customerId, cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(customerId);
        if (customer.CompanyId != input.CompanyId.GetValueOrDefault())
        {
            throw new BaseHttpException(400, "The customer is not of this company.").WithKey("subscription_company_mismatch", customerId);
        }

        // a reseller sells as itself; the system tenant names the reseller it sells for
        Guid tenantId = CurrentTenant.IsSystemTenant
            ? input.TenantId ?? throw new BaseHttpException(400, "The system tenant names the reseller that sells.").WithKey("subscription_tenant_required")
            : input.TenantId is { } named && named != CurrentTenant.Id
                ? throw new BaseHttpException(403, "A tenant sells only as itself.").WithKey("subscription_tenant_not_allowed", named)
                : CurrentTenant.Id ?? throw new BaseHttpException(403, "The caller has no tenant.").WithKey("subscription_tenant_required");
        Subscription entity = Subscription.Create(tenantId, input.CompanyId.GetValueOrDefault(), input.CustomerId.GetValueOrDefault(), input.ProductType.GetValueOrDefault(), input.ValidTo, input.IsBlocked, input.SettingsJson);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SubscriptionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Subscription entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.ValidTo, input.IsBlocked, input.SettingsJson);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<Subscription> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw SubscriptionNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Subscription, bool>> Filter(string? searchText, Guid? tenantId, Guid? companyId, Guid? customerId, ProductTypes? productType)
    {
        string? search = Normalized(searchText);
        return new FilterBuilder<Subscription>()
            .And(search is null ? null : entity => entity.ScopeKey.Contains(search))
            .And(tenantId is null ? null : entity => entity.TenantId == tenantId.Value)
            .And(companyId is null ? null : entity => entity.CompanyId == companyId.Value)
            .And(customerId is null ? null : entity => entity.CustomerId == customerId.Value)
            .And(productType is null ? null : entity => entity.ProductType == productType.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
