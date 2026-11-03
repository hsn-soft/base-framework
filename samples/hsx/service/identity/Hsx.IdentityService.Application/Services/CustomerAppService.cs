using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Services;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Exceptions;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Exceptions;
using Hsx.IdentityService.Domain.CustomerDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

public sealed class CustomerAppService(IRepository<Customer, Guid> repository, IRepository<Company, Guid> companies, IServiceProvider services) : ApplicationServiceBase(services), ICustomerAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<CustomerDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(id);

    public async Task<PagedDataResultDto<CustomerDto>> GetPagedListAsync(GetCustomersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CustomerDto> page = await repository.GetPagedListAsync<CustomerDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.CompanyId), CustomerSorting.Fields, CustomerSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<CustomerDto> CreateAsync(CustomerCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureSystemTenant();

        Guid companyId = input.CompanyId.GetValueOrDefault();
        if (!await companies.AnyAsync(company => company.Id == companyId, ignoreFilters: false, cancellationToken).ConfigureAwait(false))
        {
            throw CompanyNotFound.For(companyId);
        }

        string domain = input.Domain!.Trim();
        if (await DomainTakenAsync(domain, cancellationToken).ConfigureAwait(false))
        {
            throw new CustomerDomainDuplicateException(domain);
        }

        Customer entity = Customer.Create(companyId, domain);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureSystemTenant();
        await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    private async Task<Customer> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(id);

    // a domain is unique: a deleted customer's domain is taken too
    private Task<bool> DomainTakenAsync(string domain, CancellationToken cancellationToken)
    {
        string normalized = StringHelper.Normalize(domain);
        return repository.AnyAsync(entity => entity.NormalizedDomain == normalized, ignoreFilters: true, cancellationToken);
    }

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Customer, bool>> Filter(string? searchText, Guid? companyId)
    {
        string? search = Normalized(searchText);
        return new FilterBuilder<Customer>()
            .And(search is null ? null : entity => entity.NormalizedDomain.Contains(search))
            .And(companyId is null ? null : entity => entity.CompanyId == companyId.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
