using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Filters;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos.Submits;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Services;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Entities;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Exceptions;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Repositories;

namespace Hsx.TextNormalizerService.Application.Services;

public sealed class CustomerContentNormalizedRequestAppService(IRepository<CustomerContentNormalizedRequest, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), ICustomerContentNormalizedRequestAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CustomerContentNormalizedRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<CustomerContentNormalizedRequestDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerContentNormalizedRequestNotFound.For(id);

    public async Task<PagedDataResultDto<CustomerContentNormalizedRequestDto>> GetPagedListAsync(GetCustomerContentNormalizedRequestsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CustomerContentNormalizedRequestDto> page = await repository.GetPagedListAsync<CustomerContentNormalizedRequestDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.CustomerContentId, pagedInput.Status, pagedInput.ScrapingStatus, pagedInput.OutlineStatus), CustomerContentNormalizedRequestSorting.Fields, CustomerContentNormalizedRequestSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<CustomerContentNormalizedRequestDto> CreateAsync(CustomerContentNormalizedRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        CustomerContentNormalizedRequest entity = CustomerContentNormalizedRequest.Create(input.ScopeKey!, input.CustomerContentId.GetValueOrDefault(), input.DomainName!, input.ContentKey!, input.CorrelationId, input.Status!, input.CurrentMilestone!, input.ScrapingStatus, input.OutlineStatus, input.RetryCount, input.LastError);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerContentNormalizedRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        CustomerContentNormalizedRequest entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.Status!, input.CurrentMilestone!, input.ScrapingStatus, input.OutlineStatus, input.RetryCount, input.LastError);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<CustomerContentNormalizedRequest> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CustomerContentNormalizedRequestNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<CustomerContentNormalizedRequest, bool>> Filter(string? searchText, string? scopeKey, Guid? customerContentId, string? status, string? scrapingStatus, string? outlineStatus)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        string? statusText = Trimmed(status);
        string? scrapingStatusText = Trimmed(scrapingStatus);
        string? outlineStatusText = Trimmed(outlineStatus);
        return new FilterBuilder<CustomerContentNormalizedRequest>()
            .And(search is null ? null : entity => entity.DomainName.Contains(search) || entity.NormalizedContentKey.Contains(search))
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(customerContentId is null ? null : entity => entity.CustomerContentId == customerContentId.Value)
            .And(statusText is null ? null : entity => entity.Status == statusText)
            .And(scrapingStatusText is null ? null : entity => entity.ScrapingStatus == scrapingStatusText)
            .And(outlineStatusText is null ? null : entity => entity.OutlineStatus == outlineStatusText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
