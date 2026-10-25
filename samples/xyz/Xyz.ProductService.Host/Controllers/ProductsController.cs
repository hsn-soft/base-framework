using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xyz.ProductService.Application.Contracts.ProductDomain.Dtos;
using Xyz.ProductService.Application.Contracts.ProductDomain.Dtos.Filters;
using Xyz.ProductService.Application.Contracts.ProductDomain.Dtos.Submits;
using Xyz.ProductService.Application.Contracts.ProductDomain.Services;
using Xyz.ProductService.Controllers.Base;

namespace Xyz.ProductService.Controllers;

/// <summary>The products: every action only calls the application service, which holds the rules.</summary>
[Route("api/product-service/v1/commercial/products")]
public sealed class ProductsController(IProductAppService productAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => productAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<ProductDto>> GetPagedListAsync([FromBody] GetProductsPaged pagedInput, CancellationToken cancellationToken = default)
        => productAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ProductDto>> GetFilterListAsync([FromBody] GetProductsFilter filterInput, CancellationToken cancellationToken = default)
        => productAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ProductSearchDto>> GetSearchListAsync([FromBody] GetProductsSearch searchInput, CancellationToken cancellationToken = default)
        => productAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] ProductCreateDto input, CancellationToken cancellationToken = default)
    {
        ProductDto created = await productAppService.CreateAsync(input, cancellationToken);
        Logger.LogInformation("Product {CreatedProductId} was created", created.Id);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] ProductUpdateDto input, CancellationToken cancellationToken = default)
    {
        await productAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await productAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
