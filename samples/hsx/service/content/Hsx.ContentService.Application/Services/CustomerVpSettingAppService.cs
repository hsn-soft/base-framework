using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Services;
using Hsx.ContentService.Domain.CustomerVpSettingDomain.Entities;
using Hsx.ContentService.Domain.CustomerVpSettingDomain.Exceptions;
using Hsx.ContentService.Domain.CustomerVpSettingDomain.Repositories;

namespace Hsx.ContentService.Application.Services;

public sealed class CustomerVpSettingAppService(IRepository<CustomerVpSetting, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), ICustomerVpSettingAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CustomerVpSettingDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<CustomerVpSettingDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerVpSettingNotFound.For(id);

    public async Task<PagedDataResultDto<CustomerVpSettingDto>> GetPagedListAsync(GetCustomerVpSettingsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CustomerVpSettingDto> page = await repository.GetPagedListAsync<CustomerVpSettingDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.IsBlocked), CustomerVpSettingSorting.Fields, CustomerVpSettingSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<CustomerVpSettingDto> CreateAsync(CustomerVpSettingCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        CustomerVpSetting entity = CustomerVpSetting.Create(input.ScopeKey!, input.DomainName!, input.IsBlocked, input.DailyDirectVideoGenerationLimit, input.DailyTrendVideoGenerationLimit, input.DailyAnalysisVideoGenerationLimit, input.IsAnalysisSourcesEnabled);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerVpSettingUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        CustomerVpSetting entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.IsBlocked, input.DailyDirectVideoGenerationLimit, input.DailyTrendVideoGenerationLimit, input.DailyAnalysisVideoGenerationLimit, input.IsAnalysisSourcesEnabled);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<CustomerVpSetting> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CustomerVpSettingNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<CustomerVpSetting, bool>> Filter(string? searchText, string? scopeKey, bool? isBlocked)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        return new FilterBuilder<CustomerVpSetting>()
            .And(search is null ? null : entity => entity.NormalizedDomainName.Contains(search))
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(isBlocked is null ? null : entity => entity.IsBlocked == isBlocked.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
