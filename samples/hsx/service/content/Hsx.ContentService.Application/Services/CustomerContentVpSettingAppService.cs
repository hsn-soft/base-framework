using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Filters;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Dtos.Submits;
using Hsx.ContentService.Application.Contracts.CustomerContentVpSettingDomain.Services;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Exceptions;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Repositories;

namespace Hsx.ContentService.Application.Services;

public sealed class CustomerContentVpSettingAppService(IRepository<CustomerContentVpSetting, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), ICustomerContentVpSettingAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CustomerContentVpSettingDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<CustomerContentVpSettingDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerContentVpSettingNotFound.For(id);

    public async Task<PagedDataResultDto<CustomerContentVpSettingDto>> GetPagedListAsync(GetCustomerContentVpSettingsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CustomerContentVpSettingDto> page = await repository.GetPagedListAsync<CustomerContentVpSettingDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.IsBlocked), CustomerContentVpSettingSorting.Fields, CustomerContentVpSettingSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<CustomerContentVpSettingDto> CreateAsync(CustomerContentVpSettingCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        CustomerContentVpSetting entity = CustomerContentVpSetting.Create(input.ScopeKey!, input.DomainName!, input.IsBlocked, input.DailyDirectVideoGenerationLimit, input.DailyTrendVideoGenerationLimit, input.DailyAnalysisVideoGenerationLimit, input.IsAnalysisSourcesEnabled);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerContentVpSettingUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        CustomerContentVpSetting entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.IsBlocked, input.DailyDirectVideoGenerationLimit, input.DailyTrendVideoGenerationLimit, input.DailyAnalysisVideoGenerationLimit, input.IsAnalysisSourcesEnabled);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<CustomerContentVpSetting> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CustomerContentVpSettingNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<CustomerContentVpSetting, bool>> Filter(string? searchText, string? scopeKey, bool? isBlocked)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        return new FilterBuilder<CustomerContentVpSetting>()
            .And(search is null ? null : entity => entity.NormalizedDomainName.Contains(search))
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(isBlocked is null ? null : entity => entity.IsBlocked == isBlocked.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
