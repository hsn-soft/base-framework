using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Xyz.ProductService.Application.Contracts.ProductDomain.Dtos;
using Xyz.ProductService.Application.Contracts.ProductDomain.Dtos.Filters;
using Xyz.ProductService.Application.Contracts.ProductDomain.Dtos.Submits;
using Xyz.ProductService.Application.Contracts.ProductDomain.Services;
using Xyz.ProductService.Domain.ProductDomain.Entities;
using Xyz.ProductService.Domain.ProductDomain.Exceptions;
using Xyz.ProductService.Domain.ProductDomain.Repositories;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Consts;
using Xyz.ProductService.Domain.ProductStockHistoryDomain.Entities;

namespace Xyz.ProductService.Application.Services;

public sealed class ProductAppService(IRepository<Product, Guid> products, IRepository<ProductStockHistory, Guid> stockHistory, IServiceProvider services) : ApplicationServiceBase(services), IProductAppService
{
    public async Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<ProductDto>> GetPagedListAsync(GetProductsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<Product> page = await products.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.Name, pagedInput.Sku, pagedInput.IsActive, pagedInput.InStock), ProductSorting.Fields, ProductSorting.Default), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    public async Task<List<ProductDto>> GetFilterListAsync(GetProductsFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Product> all = await products.GetListAsync(
            filterInput.ToListOptions(Filter(null, filterInput.Name, filterInput.Sku, filterInput.IsActive, filterInput.InStock), ProductSorting.Fields, ProductSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<ProductSearchDto>> GetSearchListAsync(GetProductsSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<Product> found = await products.GetListAsync(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null, null, null), ProductSorting.Fields, ProductSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(product => new ProductSearchDto { Id = product.Id, Name = product.Name, Sku = product.Sku })];
    }

    public async Task<ProductDto> CreateAsync(ProductCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // a sku is unique in the whole system: a deleted product's sku and another tenant's are taken too
        string sku = StringHelper.Normalize(input.Sku);
        if (await products.AnyAsync(product => product.Sku == sku, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new ProductSkuDuplicateException(input.Sku.Trim());
        }

        Product product = Product.Create(input.Name, input.Sku, input.Description, input.Price.GetValueOrDefault(), input.Stock.GetValueOrDefault());
        await products.AddAsync(product, cancellationToken).ConfigureAwait(false);
        await RecordStockAsync(product, 0, ProductStockHistoryConsts.Created, cancellationToken).ConfigureAwait(false);
        return ToDto(product);
    }

    public async Task UpdateAsync(ProductUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Product product = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        int stockBefore = product.Stock;
        product.Update(input.Name, input.Description, input.Price.GetValueOrDefault(), input.Stock.GetValueOrDefault(), input.IsActive.GetValueOrDefault());
        await products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        if (product.Stock != stockBefore)
        {
            await RecordStockAsync(product, stockBefore, ProductStockHistoryConsts.Updated, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Product product = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        await products.DeleteAsync(product, cancellationToken).ConfigureAwait(false);
    }

    // every change of the stock is written to the stock history (its own MongoDB): there is no transaction over the two, so a failure here leaves the change made and the record not
    private Task RecordStockAsync(Product product, int stockBefore, string reason, CancellationToken cancellationToken)
        => stockHistory.AddAsync(ProductStockHistory.Record(product.Id, product.Name, stockBefore, product.Stock, reason), cancellationToken);

    private async Task<Product> FindAsync(Guid id, CancellationToken cancellationToken)
        => await products.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw ProductNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Product, bool>> Filter(string? searchText, string? name, string? sku, bool? isActive, bool? inStock)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(name);
        string? skuText = Normalized(sku);
        return new FilterBuilder<Product>()
            .And(search is null ? null : product => product.NormalizedName.Contains(search) || product.Sku.Contains(search))
            .And(nameText is null ? null : product => product.NormalizedName.Contains(nameText))
            .And(skuText is null ? null : product => product.Sku.Contains(skuText))
            .And(isActive is null ? null : product => product.IsActive == isActive.Value)
            .And(inStock is null ? null : inStock.Value ? product => product.Stock > 0 : product => product.Stock <= 0)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Sku = product.Sku,
        Description = product.Description,
        Price = product.Price,
        Stock = product.Stock,
        IsActive = product.IsActive,
        TenantId = product.TenantId,
        CreatedAt = product.CreatedAt,
        CreatedBy = product.CreatedBy,
        UpdatedAt = product.UpdatedAt,
        UpdatedBy = product.UpdatedBy,
    };
}
