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
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

public sealed class SubscriptionAppService(IRepository<Subscription, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), ISubscriptionAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<SubscriptionDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw SubscriptionNotFound.For(id);

    public async Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync(GetSubscriptionsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<SubscriptionDto> page = await repository.GetPagedListAsync<SubscriptionDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ResellerTenantId, pagedInput.CompanyId, pagedInput.CustomerId, pagedInput.ProductType), SubscriptionSorting.Fields, SubscriptionSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<SubscriptionDto> CreateAsync(SubscriptionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Subscription entity = Subscription.Create(input.ResellerTenantId.GetValueOrDefault(), input.CompanyId.GetValueOrDefault(), input.CustomerId.GetValueOrDefault(), input.ProductType.GetValueOrDefault(), input.ValidTo, input.IsBlocked, input.SettingsJson);
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
    private static Expression<Func<Subscription, bool>> Filter(string? searchText, Guid? resellerTenantId, Guid? companyId, Guid? customerId, ProductTypes? productType)
    {
        string? search = Normalized(searchText);
        return new FilterBuilder<Subscription>()
            .And(search is null ? null : entity => entity.ScopeKey.Contains(search))
            .And(resellerTenantId is null ? null : entity => entity.ResellerTenantId == resellerTenantId.Value)
            .And(companyId is null ? null : entity => entity.CompanyId == companyId.Value)
            .And(customerId is null ? null : entity => entity.CustomerId == customerId.Value)
            .And(productType is null ? null : entity => entity.ProductType == productType.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
