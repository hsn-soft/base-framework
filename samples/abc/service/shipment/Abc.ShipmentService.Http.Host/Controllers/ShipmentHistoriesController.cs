using Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Filters;
using Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Services;
using Abc.ShipmentService.Http.Host.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Abc.ShipmentService.Http.Host.Controllers;

/// <summary>The history of the shipments (kept in its own database, with EF Core on PostgreSQL): every action only calls the application service, which holds the rules.</summary>
[Route("api/shipment-service/v1/commercial/shipment-histories")]
public sealed class ShipmentHistoriesController(IShipmentHistoryAppService historyAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ShipmentHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => historyAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<ShipmentHistoryDto>> GetPagedListAsync([FromBody] GetShipmentHistoriesPaged pagedInput, CancellationToken cancellationToken = default)
        => historyAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ShipmentHistoryDto>> GetFilterListAsync([FromBody] GetShipmentHistoriesFilter filterInput, CancellationToken cancellationToken = default)
        => historyAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ShipmentHistorySearchDto>> GetSearchListAsync([FromBody] GetShipmentHistoriesSearch searchInput, CancellationToken cancellationToken = default)
        => historyAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] ShipmentHistoryCreateDto input, CancellationToken cancellationToken = default)
    {
        ShipmentHistoryDto created = await historyAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] ShipmentHistoryUpdateDto input, CancellationToken cancellationToken = default)
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
