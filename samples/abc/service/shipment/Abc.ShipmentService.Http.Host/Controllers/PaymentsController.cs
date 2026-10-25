using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Filters;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Services;
using Abc.ShipmentService.Http.Host.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Abc.ShipmentService.Http.Host.Controllers;

/// <summary>The payments: every action only calls the application service, which holds the rules.</summary>
[Route("api/shipment-service/v1/commercial/payments")]
public sealed class PaymentsController(IPaymentAppService paymentAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PaymentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => paymentAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<PaymentDto>> GetPagedListAsync([FromBody] GetPaymentsPaged pagedInput, CancellationToken cancellationToken = default)
        => paymentAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<PaymentDto>> GetFilterListAsync([FromBody] GetPaymentsFilter filterInput, CancellationToken cancellationToken = default)
        => paymentAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<PaymentSearchDto>> GetSearchListAsync([FromBody] GetPaymentsSearch searchInput, CancellationToken cancellationToken = default)
        => paymentAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] PaymentCreateDto input, CancellationToken cancellationToken = default)
    {
        PaymentDto created = await paymentAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] PaymentUpdateDto input, CancellationToken cancellationToken = default)
    {
        await paymentAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await paymentAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
