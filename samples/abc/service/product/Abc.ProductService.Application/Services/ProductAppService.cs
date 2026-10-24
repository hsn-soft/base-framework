using Abc.ProductService.Application.Contracts.Dtos;
using Abc.ProductService.Application.Contracts.ProductDomain.Dtos;
using Abc.ProductService.Application.Contracts.ProductDomain.Dtos.Filters;
using Abc.ProductService.Application.Contracts.ProductDomain.Dtos.Submits;
using Abc.ProductService.Application.Contracts.ProductDomain.Services;
using Abc.ProductService.Application.Security;
using Abc.ProductService.Domain.ProductDomain.Entities;
using Abc.ProductService.Domain.ProductDomain.Exceptions;
using Abc.ProductService.Domain.ProductDomain.Repositories;

namespace Abc.ProductService.Application.Services;

public sealed class ProductAppService(IProductRepository products, TimeProvider clock, ICurrentUser currentUser) : IProductAppService
{
    public async Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<ProductDto>> GetPagedListAsync(GetProductsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        ProductFilter filter = new(pagedInput.Name, pagedInput.Sku, pagedInput.IsActive, pagedInput.InStock);
        IReadOnlyList<Product> page = await products.GetListAsync(filter, pagedInput.Skip, pagedInput.Take, cancellationToken).ConfigureAwait(false);
        long total = await products.CountAsync(filter, cancellationToken).ConfigureAwait(false);
        return new PagedDataResultDto<ProductDto> { Items = [.. page.Select(ToDto)], TotalCount = (int)total, Skip = pagedInput.Skip, Take = pagedInput.Take };
    }

    public async Task<List<ProductDto>> GetFilterListAsync(GetProductsFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        ProductFilter filter = new(filterInput.Name, filterInput.Sku, filterInput.IsActive, filterInput.InStock);
        IReadOnlyList<Product> all = await products.GetListAsync(filter, 0, filterInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<ProductSearchDto>> GetSearchListAsync(GetProductsSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<Product> found = await products.SearchAsync(searchInput.SearchText, searchInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. found.Select(product => new ProductSearchDto { Id = product.Id, Name = product.Name, Sku = product.Sku })];
    }

    public async Task<ProductDto> CreateAsync(ProductCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (await products.SkuExistsAsync(input.Sku, null, cancellationToken).ConfigureAwait(false))
        {
            throw new ProductSkuDuplicateException(input.Sku.Trim());
        }

        Product product = Product.Create(input.Name, input.Sku, input.Description, input.Price, input.Stock, clock.GetUtcNow(), currentUser.UserName);
        await products.AddAsync(product, cancellationToken).ConfigureAwait(false);
        return ToDto(product);
    }

    public async Task UpdateAsync(ProductUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Product product = await FindAsync(input.Id, cancellationToken).ConfigureAwait(false);
        product.Update(input.Name, input.Description, input.Price, input.Stock, input.IsActive, clock.GetUtcNow(), currentUser.UserName);
        await products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Product product = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        await products.DeleteAsync(product, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Product> FindAsync(Guid id, CancellationToken cancellationToken)
        => await products.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw ProductNotFound.For(id);

    private static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Sku = product.Sku,
        Description = product.Description,
        Price = product.Price,
        Stock = product.Stock,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        CreatedBy = product.CreatedBy,
        UpdatedAt = product.UpdatedAt,
        UpdatedBy = product.UpdatedBy,
    };
}
