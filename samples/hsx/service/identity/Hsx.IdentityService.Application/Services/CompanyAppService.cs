using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.CompanyDomain.Services;
using Hsx.IdentityService.Domain.CompanyDomain.Consts;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Exceptions;

namespace Hsx.IdentityService.Application.Services;

public sealed class CompanyAppService(IRepository<Company, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), ICompanyAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CompanyDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<CompanyDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CompanyNotFound.For(id);

    public async Task<PagedDataResultDto<CompanyDto>> GetPagedListAsync(GetCompaniesPagedDto pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CompanyDto> page = await repository.GetPagedListAsync<CompanyDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText), CompanySorting.Fields, CompanySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<CompanyDto> CreateAsync(CompanyCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        string name = input.Name!.Trim();
        if (await NameTakenAsync(name, cancellationToken).ConfigureAwait(false))
        {
            throw new CompanyNameDuplicateException(name);
        }

        Company entity = Company.Create(input.Title!, name);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CompanyDto> UpdateAsync(CompanyUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        Company entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.Title!);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CompanyDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureSystemTenant();
        CompanyDto deleted = await GetAsync(id, cancellationToken).ConfigureAwait(false);
        await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        return deleted;
    }

    private async Task<Company> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CompanyNotFound.For(id);

    // a name is unique: a deleted company's name is taken too
    private Task<bool> NameTakenAsync(string name, CancellationToken cancellationToken)
    {
        string normalized = StringHelper.Normalize(name);
        return repository.AnyAsync(entity => entity.NormalizedName == normalized, ignoreFilters: true, cancellationToken);
    }

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Company, bool>> Filter(string? searchText)
    {
        string? search = Normalized(searchText);
        return new FilterBuilder<Company>()
            .And(search is null ? null : entity => entity.Title.Contains(search) || entity.NormalizedName.Contains(search))
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
