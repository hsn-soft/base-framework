using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Services;
using Hsx.ContentService.Domain.AnalysisContentDomain.Entities;
using Hsx.ContentService.Domain.AnalysisContentDomain.Exceptions;
using Hsx.ContentService.Domain.AnalysisContentDomain.Repositories;

namespace Hsx.ContentService.Application.Services;

public sealed class AnalysisContentAppService(IRepository<AnalysisContent, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), IAnalysisContentAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<AnalysisContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<AnalysisContentDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw AnalysisContentNotFound.For(id);

    public async Task<PagedDataResultDto<AnalysisContentDto>> GetPagedListAsync(GetAnalysisContentsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<AnalysisContentDto> page = await repository.GetPagedListAsync<AnalysisContentDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.NormalizeStatus, pagedInput.VideoStatus), AnalysisContentSorting.Fields, AnalysisContentSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<AnalysisContentDto> CreateAsync(AnalysisContentCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        AnalysisContent entity = AnalysisContent.Create(input.ScopeKey!, input.AnalysisDate.GetValueOrDefault(), input.NormalizeStatus, input.VideoStatus, input.VideoCdnUrl, input.CurrentMilestone, input.LastError);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AnalysisContentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AnalysisContent entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.NormalizeStatus, input.VideoStatus, input.VideoCdnUrl, input.CurrentMilestone, input.LastError);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<AnalysisContent> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw AnalysisContentNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<AnalysisContent, bool>> Filter(string? searchText, string? scopeKey, string? normalizeStatus, string? videoStatus)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        string? normalizeStatusText = Trimmed(normalizeStatus);
        string? videoStatusText = Trimmed(videoStatus);
        return new FilterBuilder<AnalysisContent>()
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(normalizeStatusText is null ? null : entity => entity.NormalizeStatus == normalizeStatusText)
            .And(videoStatusText is null ? null : entity => entity.VideoStatus == videoStatusText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
