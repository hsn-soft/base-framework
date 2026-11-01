using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Services;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Entities;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Exceptions;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Repositories;

namespace Hsx.ContentService.Application.Services;

public sealed class AnalysisContentItemAppService(IRepository<AnalysisContentItem, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), IAnalysisContentItemAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<AnalysisContentItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<AnalysisContentItemDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw AnalysisContentItemNotFound.For(id);

    public async Task<PagedDataResultDto<AnalysisContentItemDto>> GetPagedListAsync(GetAnalysisContentItemsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<AnalysisContentItemDto> page = await repository.GetPagedListAsync<AnalysisContentItemDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.AnalysisContentId, pagedInput.CustomerContentId), AnalysisContentItemSorting.Fields, AnalysisContentItemSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<AnalysisContentItemDto> CreateAsync(AnalysisContentItemCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        AnalysisContentItem entity = AnalysisContentItem.Create(input.ScopeKey!, input.AnalysisContentId.GetValueOrDefault(), input.CustomerContentId.GetValueOrDefault(), input.SortOrder, input.SourceTitle, input.SourceDescription, input.SourceImageUrl);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AnalysisContentItemUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AnalysisContentItem entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.SortOrder, input.SourceTitle, input.SourceDescription, input.SourceImageUrl);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<AnalysisContentItem> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw AnalysisContentItemNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<AnalysisContentItem, bool>> Filter(string? searchText, string? scopeKey, Guid? analysisContentId, Guid? customerContentId)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        return new FilterBuilder<AnalysisContentItem>()
            .And(search is null ? null : entity => (entity.SourceTitle != null && entity.SourceTitle.Contains(search)))
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(analysisContentId is null ? null : entity => entity.AnalysisContentId == analysisContentId.Value)
            .And(customerContentId is null ? null : entity => entity.CustomerContentId == customerContentId.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
