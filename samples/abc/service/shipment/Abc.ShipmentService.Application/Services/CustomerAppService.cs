using System.Linq.Expressions;
using Abc.ShipmentService.Application.Contracts.CustomerDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Abc.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.CustomerDomain.Services;
using Abc.ShipmentService.Domain.CustomerDomain.Entities;
using Abc.ShipmentService.Domain.CustomerDomain.Exceptions;
using Abc.ShipmentService.Domain.CustomerDomain.Repositories;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace Abc.ShipmentService.Application.Services;

public sealed class CustomerAppService(IRepository<Customer, Guid> customers, IServiceProvider services) : ApplicationServiceBase(services), ICustomerAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await customers.FindAsync<CustomerDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(id);

    public async Task<PagedDataResultDto<CustomerDto>> GetPagedListAsync(GetCustomersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<CustomerDto> page = await customers.GetPagedListAsync<CustomerDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.UserName, pagedInput.FullName, pagedInput.Email), CustomerSorting.Fields, CustomerSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<List<CustomerDto>> GetFilterListAsync(GetCustomersFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        return [.. await customers.GetListAsync<CustomerDto>(
            filterInput.ToListOptions(Filter(null, filterInput.UserName, filterInput.FullName, filterInput.Email), CustomerSorting.Fields, CustomerSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<List<CustomerSearchDto>> GetSearchListAsync(GetCustomersSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        return [.. await customers.GetListAsync<CustomerSearchDto>(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null, null), CustomerSorting.Fields, CustomerSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<CustomerDto> CreateAsync(CustomerCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string userName = input.UserName!.Trim();
        if (await IsTakenAsync(userName, cancellationToken).ConfigureAwait(false))
        {
            throw new CustomerUserNameDuplicateException(userName);
        }

        Customer customer = Customer.Create(userName, input.FullName!, input.Email, input.Phone, input.Address);
        await customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);
        return await GetAsync(customer.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerDto> EnsureAsync(string userName, string? address, CancellationToken cancellationToken = default)
    {
        string normalized = StringHelper.Normalize(userName);
        Customer? existing = await customers.FindAsync(customer => customer.NormalizedUserName == normalized, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return await GetAsync(existing.Id, cancellationToken).ConfigureAwait(false);
        }

        Customer created = Customer.Create(userName, userName, null, null, address, CurrentUser.Id);
        await customers.AddAsync(created, cancellationToken).ConfigureAwait(false);
        return await GetAsync(created.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CustomerUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Customer customer = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        customer.Update(input.FullName!, input.Email, input.Phone, input.Address);
        await customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await customers.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<Customer> FindAsync(Guid id, CancellationToken cancellationToken)
        => await customers.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw CustomerNotFound.For(id);

    // a user name is unique in the tenant: a deleted customer's name is taken too
    private Task<bool> IsTakenAsync(string userName, CancellationToken cancellationToken)
    {
        string normalized = StringHelper.Normalize(userName);
        return customers.AnyAsync(customer => customer.NormalizedUserName == normalized, ignoreFilters: true, cancellationToken);
    }

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Customer, bool>> Filter(string? searchText, string? userName, string? fullName, string? email)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(userName);
        string? fullText = Normalized(fullName);
        string? emailText = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        return new FilterBuilder<Customer>()
            .And(search is null ? null : customer => customer.NormalizedUserName.Contains(search) || customer.NormalizedFullName.Contains(search))
            .And(nameText is null ? null : customer => customer.NormalizedUserName.Contains(nameText))
            .And(fullText is null ? null : customer => customer.NormalizedFullName.Contains(fullText))
            .And(emailText is null ? null : customer => customer.Email != null && customer.Email.Contains(emailText))
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
