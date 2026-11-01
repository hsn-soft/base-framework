using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Services;
using Hsx.ContentService.Domain.CustomerContentDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentDomain.Exceptions;
using Hsx.ContentService.Domain.CustomerContentDomain.Repositories;

namespace Hsx.ContentService.Application.Services;

public sealed class CustomerContentAppService(IRepository<CustomerContent, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), ICustomerContentAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CustomerContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<CustomerContentDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerContentNotFound.For(id);

    public async Task<PagedDataResultDto<CustomerContentDto>> GetPagedListAsync(GetCustomerContentsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CustomerContentDto> page = await repository.GetPagedListAsync<CustomerContentDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.NormalizeStatus, pagedInput.VideoStatus), CustomerContentSorting.Fields, CustomerContentSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<CustomerContentDto> CreateAsync(CustomerContentCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        CustomerContent entity = CustomerContent.Create(input.ScopeKey!, input.ContentKey!, input.SlugKey!, input.NormalizeStatus, input.VideoStatus, input.VideoCdnUrl, input.CurrentMilestone, input.LastError);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerContentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        CustomerContent entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.SlugKey!, input.NormalizeStatus, input.VideoStatus, input.VideoCdnUrl, input.CurrentMilestone, input.LastError);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<CustomerContent> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CustomerContentNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<CustomerContent, bool>> Filter(string? searchText, string? scopeKey, string? normalizeStatus, string? videoStatus)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        string? normalizeStatusText = Trimmed(normalizeStatus);
        string? videoStatusText = Trimmed(videoStatus);
        return new FilterBuilder<CustomerContent>()
            .And(search is null ? null : entity => entity.NormalizedContentKey.Contains(search) || entity.SlugKey.Contains(search))
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(normalizeStatusText is null ? null : entity => entity.NormalizeStatus == normalizeStatusText)
            .And(videoStatusText is null ? null : entity => entity.VideoStatus == videoStatusText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
