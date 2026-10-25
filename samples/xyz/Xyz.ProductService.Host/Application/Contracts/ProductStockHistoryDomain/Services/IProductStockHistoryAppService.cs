using HsnSoft.Base.Application.Dtos;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Filters;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Submits;

namespace Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Services;

/// <summary>What can be done with the stock history of the products. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IProductStockHistoryAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task<ProductStockHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the records that match the filters, the latest first unless a sorting is given, with how many match in all.</summary>
    Task<PagedDataResultDto<ProductStockHistoryDto>> GetPagedListAsync(GetProductStockHistoriesPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>All the records that match the filters (at most <c>Limit</c>), not paged.</summary>
    Task<List<ProductStockHistoryDto>> GetFilterListAsync(GetProductStockHistoriesFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The records that have the text in their product name or reason, shown briefly.</summary>
    Task<List<ProductStockHistorySearchDto>> GetSearchListAsync(GetProductStockHistoriesSearch searchInput, CancellationToken cancellationToken = default);

    Task<ProductStockHistoryDto> CreateAsync(ProductStockHistoryCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task UpdateAsync(ProductStockHistoryUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such record.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
