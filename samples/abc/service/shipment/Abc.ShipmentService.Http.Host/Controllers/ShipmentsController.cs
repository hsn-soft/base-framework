using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Filters;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Abc.ShipmentService.Http.Host.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Abc.ShipmentService.Http.Host.Controllers;

/// <summary>The shipments: every action only calls the application service, which holds the rules.</summary>
[Route("api/shipment-service/v1/commercial/shipments")]
public sealed class ShipmentsController(IShipmentAppService shipmentAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ShipmentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => shipmentAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<ShipmentDto>> GetPagedListAsync([FromBody] GetShipmentsPaged pagedInput, CancellationToken cancellationToken = default)
        => shipmentAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ShipmentDto>> GetFilterListAsync([FromBody] GetShipmentsFilter filterInput, CancellationToken cancellationToken = default)
        => shipmentAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<ShipmentSearchDto>> GetSearchListAsync([FromBody] GetShipmentsSearch searchInput, CancellationToken cancellationToken = default)
        => shipmentAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] ShipmentCreateDto input, CancellationToken cancellationToken = default)
    {
        ShipmentDto created = await shipmentAppService.CreateAsync(input, cancellationToken);
        Logger.LogInformation("Shipment {CreatedShipmentId} was created", created.Id);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] ShipmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        await shipmentAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await shipmentAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
