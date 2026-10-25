using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Filters;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Dtos.Submits;
using Xyz.ShipmentService.Application.Contracts.CustomerDomain.Services;
using Xyz.ShipmentService.Controllers.Base;

namespace Xyz.ShipmentService.Controllers;

/// <summary>The customers: every action only calls the application service, which holds the rules.</summary>
[Route("api/shipment-service/v1/commercial/customers")]
public sealed class CustomersController(ICustomerAppService customerAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => customerAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<CustomerDto>> GetPagedListAsync([FromBody] GetCustomersPaged pagedInput, CancellationToken cancellationToken = default)
        => customerAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<CustomerDto>> GetFilterListAsync([FromBody] GetCustomersFilter filterInput, CancellationToken cancellationToken = default)
        => customerAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<CustomerSearchDto>> GetSearchListAsync([FromBody] GetCustomersSearch searchInput, CancellationToken cancellationToken = default)
        => customerAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CustomerCreateDto input, CancellationToken cancellationToken = default)
    {
        CustomerDto created = await customerAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] CustomerUpdateDto input, CancellationToken cancellationToken = default)
    {
        await customerAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await customerAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
