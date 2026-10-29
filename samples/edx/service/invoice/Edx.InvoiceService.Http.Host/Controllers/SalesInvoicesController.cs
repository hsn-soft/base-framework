using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Submits;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Services;
using Edx.InvoiceService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.InvoiceService.Http.Host.Controllers;

/// <summary>
/// The invoices of a tenant, from the draft on, with every kind of permission at work. The attribute of each action is its endpoint permission (403 when the role was not granted it). What the role may see and how much it may see or approve are asked inside the application
/// service (<see cref="ISalesInvoiceAppService"/>), as operations and constraints. Invoices are not made here: they come from the queue of the firm invoice integration.
/// </summary>
[Route("api/invoice-service/v1/commercial/sales-invoices")]
public sealed class SalesInvoicesController(ISalesInvoiceAppService invoices) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.Invoices.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<SalesInvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => invoices.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.Invoices.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<SalesInvoiceDto>> GetPagedListAsync([FromBody] GetSalesInvoicesPaged pagedInput, CancellationToken cancellationToken = default)
        => invoices.GetPagedListAsync(pagedInput, cancellationToken);

    /// <summary>Approves a draft: it is signed afterwards. The total may not go over what the role may approve (<c>constraint.invoice.max-approve-amount</c>).</summary>
    [HttpPost("{id:guid}/approve")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.Invoices.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await invoices.ApproveAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Rejects a draft, with the reason the sender reads in the queue. The sender may send the invoice again.</summary>
    [HttpPost("{id:guid}/reject")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.Invoices.Reject)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RejectAsync(Guid id, [FromBody] InvoiceRejectDto input, CancellationToken cancellationToken = default)
    {
        await invoices.RejectAsync(id, input, cancellationToken);
        return NoContent();
    }

    /// <summary>The XML of the invoice: the signed one, else the draft. It holds what the operations hide, so the role needs both <c>operation.invoice.invoice.tax-id.view</c> and <c>operation.invoice.invoice.amounts.view</c>.</summary>
    [HttpGet("{id:guid}/xml")]
    [Produces("application/xml")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.Invoices.Xml)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetXmlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        InvoiceXmlDto xml = await invoices.GetXmlAsync(id, cancellationToken);
        return File(xml.Content, "application/xml", xml.FileName);
    }
}
