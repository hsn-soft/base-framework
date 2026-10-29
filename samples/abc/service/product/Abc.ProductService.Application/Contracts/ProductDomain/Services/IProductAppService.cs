using Abc.ProductService.Application.Contracts.ProductDomain.Dtos;
using Abc.ProductService.Application.Contracts.ProductDomain.Dtos.Filters;
using Abc.ProductService.Application.Contracts.ProductDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Abc.ProductService.Application.Contracts.ProductDomain.Services;

/// <summary>What can be done with the products. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IProductAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such product.</exception>
    Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the products that match the filters, with how many match in all.</summary>
    Task<PagedDataResultDto<ProductDto>> GetPagedListAsync(GetProductsPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>All the products that match the filters (at most <c>Limit</c>), not paged.</summary>
    Task<List<ProductDto>> GetFilterListAsync(GetProductsFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The products that have the text in them, shown briefly.</summary>
    Task<List<ProductSearchDto>> GetSearchListAsync(GetProductsSearch searchInput, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(ProductCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such product.</exception>
    Task UpdateAsync(ProductUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such product.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
