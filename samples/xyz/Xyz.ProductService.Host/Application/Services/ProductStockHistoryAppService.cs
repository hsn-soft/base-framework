using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Filters;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Submits;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Services;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Entities;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Exceptions;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Repositories;

namespace Xyz.ProductService.Application.Services;

public sealed class ProductStockHistoryAppService(IRepository<ProductStockHistory, Guid> histories, IServiceProvider services) : ApplicationServiceBase(services), IProductStockHistoryAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<ProductStockHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await histories.FindAsync<ProductStockHistoryDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw ProductStockHistoryNotFound.For(id);

    public async Task<PagedDataResultDto<ProductStockHistoryDto>> GetPagedListAsync(GetProductStockHistoriesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<ProductStockHistoryDto> page = await histories.GetPagedListAsync<ProductStockHistoryDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ProductId, pagedInput.ProductName, pagedInput.Reason), ProductStockHistorySorting.Fields, ProductStockHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<List<ProductStockHistoryDto>> GetFilterListAsync(GetProductStockHistoriesFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        return [.. await histories.GetListAsync<ProductStockHistoryDto>(
            filterInput.ToListOptions(Filter(null, filterInput.ProductId, filterInput.ProductName, filterInput.Reason), ProductStockHistorySorting.Fields, ProductStockHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<List<ProductStockHistorySearchDto>> GetSearchListAsync(GetProductStockHistoriesSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        return [.. await histories.GetListAsync<ProductStockHistorySearchDto>(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null, null), ProductStockHistorySorting.Fields, ProductStockHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<ProductStockHistoryDto> CreateAsync(ProductStockHistoryCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ProductStockHistory history = ProductStockHistory.Record(input.ProductId.GetValueOrDefault(), input.ProductName!, input.OldStock.GetValueOrDefault(), input.NewStock.GetValueOrDefault(), input.Reason!, input.Note);
        await histories.AddAsync(history, cancellationToken).ConfigureAwait(false);
        return await GetAsync(history.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProductStockHistoryUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ProductStockHistory history = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        history.Update(input.Reason!, input.Note);
        await histories.UpdateAsync(history, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await histories.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<ProductStockHistory> FindAsync(Guid id, CancellationToken cancellationToken)
        => await histories.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw ProductStockHistoryNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<ProductStockHistory, bool>> Filter(string? searchText, Guid? productId, string? productName, string? reason)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(productName);
        string? reasonText = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim().ToLowerInvariant();
        string? reasonPart = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLowerInvariant();
        return new FilterBuilder<ProductStockHistory>()
            .And(search is null ? null : history => history.NormalizedProductName.Contains(search) || history.Reason.Contains(reasonPart!))
            .And(productId is null ? null : history => history.ProductId == productId.Value)
            .And(nameText is null ? null : history => history.NormalizedProductName.Contains(nameText))
            .And(reasonText is null ? null : history => history.Reason == reasonText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
