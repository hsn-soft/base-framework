using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Services;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Exceptions;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Repositories;

namespace Hsx.ContentService.Application.Services;

public sealed class CustomerContentVisitAppService(IRepository<CustomerContentVisit, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), ICustomerContentVisitAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CustomerContentVisitDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<CustomerContentVisitDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerContentVisitNotFound.For(id);

    public async Task<PagedDataResultDto<CustomerContentVisitDto>> GetPagedListAsync(GetCustomerContentVisitsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CustomerContentVisitDto> page = await repository.GetPagedListAsync<CustomerContentVisitDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.CustomerContentId, pagedInput.VisitResponse), CustomerContentVisitSorting.Fields, CustomerContentVisitSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<CustomerContentVisitDto> CreateAsync(CustomerContentVisitCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        CustomerContentVisit entity = CustomerContentVisit.Create(input.ScopeKey!, input.CustomerContentId.GetValueOrDefault(), input.VisitResponse!);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<CustomerContentVisit> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CustomerContentVisitNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<CustomerContentVisit, bool>> Filter(string? searchText, string? scopeKey, Guid? customerContentId, string? visitResponse)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        string? visitResponseText = Trimmed(visitResponse);
        return new FilterBuilder<CustomerContentVisit>()
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(customerContentId is null ? null : entity => entity.CustomerContentId == customerContentId.Value)
            .And(visitResponseText is null ? null : entity => entity.VisitResponse == visitResponseText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
