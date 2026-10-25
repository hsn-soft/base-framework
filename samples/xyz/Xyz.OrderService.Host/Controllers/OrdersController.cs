using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xyz.OrderService.Application.Contracts.OrderDomain.Dtos;
using Xyz.OrderService.Application.Contracts.OrderDomain.Dtos.Filters;
using Xyz.OrderService.Application.Contracts.OrderDomain.Dtos.Submits;
using Xyz.OrderService.Application.Contracts.OrderDomain.Services;
using Xyz.OrderService.Controllers.Base;

namespace Xyz.OrderService.Controllers;

/// <summary>The orders: every action only calls the application service, which holds the rules.</summary>
[Route("api/order-service/v1/commercial/orders")]
public sealed class OrdersController(IOrderAppService orderAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<OrderDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => orderAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<OrderDto>> GetPagedListAsync([FromBody] GetOrdersPaged pagedInput, CancellationToken cancellationToken = default)
        => orderAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<OrderDto>> GetFilterListAsync([FromBody] GetOrdersFilter filterInput, CancellationToken cancellationToken = default)
        => orderAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<OrderSearchDto>> GetSearchListAsync([FromBody] GetOrdersSearch searchInput, CancellationToken cancellationToken = default)
        => orderAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] OrderCreateDto input, CancellationToken cancellationToken = default)
    {
        OrderDto created = await orderAppService.CreateAsync(input, cancellationToken);
        Logger.LogInformation("Order {CreatedOrderId} was created", created.Id);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] OrderUpdateDto input, CancellationToken cancellationToken = default)
    {
        await orderAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await orderAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
