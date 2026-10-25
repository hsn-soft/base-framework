using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Filters;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos.Submits;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Services;
using Xyz.ProductService.Controllers.Base;

namespace Xyz.ProductService.Controllers;

/// <summary>The stock history of the products (kept in its own MongoDB): every action only calls the application service, which holds the rules.</summary>
[Route("api/product-service/v1/commercial/product-stock-histories")]
public sealed class ProductStockHistoriesController(IProductStockHistoryAppService historyAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ProductStockHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => historyAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<ProductStockHistoryDto>> GetPagedListAsync([FromBody] GetProductStockHistoriesPaged pagedInput, CancellationToken cancellationToken = default)
        => historyAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ProductStockHistoryDto>> GetFilterListAsync([FromBody] GetProductStockHistoriesFilter filterInput, CancellationToken cancellationToken = default)
        => historyAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ProductStockHistorySearchDto>> GetSearchListAsync([FromBody] GetProductStockHistoriesSearch searchInput, CancellationToken cancellationToken = default)
        => historyAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] ProductStockHistoryCreateDto input, CancellationToken cancellationToken = default)
    {
        ProductStockHistoryDto created = await historyAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] ProductStockHistoryUpdateDto input, CancellationToken cancellationToken = default)
    {
        await historyAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await historyAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
