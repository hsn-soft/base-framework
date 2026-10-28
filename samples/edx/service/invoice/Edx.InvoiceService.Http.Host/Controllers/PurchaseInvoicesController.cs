using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Submits;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Services;
using Edx.InvoiceService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.InvoiceService.Http.Host.Controllers;

/// <summary>The invoices issued to the tenant (they came in at its post box). What the role may see and how much are asked inside the application service, as operations and constraints, as for the invoices the tenant sends.</summary>
[Route("api/invoice-service/v1/commercial/purchase-invoices")]
public sealed class PurchaseInvoicesController(IPurchaseInvoiceAppService invoices) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.PurchaseInvoices.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PurchaseInvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => invoices.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.PurchaseInvoices.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<PurchaseInvoiceDto>> GetPagedListAsync([FromBody] GetPurchaseInvoicesPaged pagedInput, CancellationToken cancellationToken = default)
        => invoices.GetPagedListAsync(pagedInput, cancellationToken);

    /// <summary>The XML of the invoice as it came. It holds what the operations hide, so the role needs both <c>operation.invoice.tax-id.view</c> and <c>operation.invoice.price.view</c>.</summary>
    [HttpPost("{id:guid}/respond")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.PurchaseInvoices.Respond)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PurchaseInvoiceDto> RespondAsync(Guid id, [FromBody] PurchaseInvoiceRespondDto input, CancellationToken cancellationToken = default)
        => invoices.RespondAsync(id, input, cancellationToken);

    [HttpGet("{id:guid}/xml")]
    [Produces("application/xml")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.PurchaseInvoices.Xml)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetXmlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        InvoiceXmlDto xml = await invoices.GetXmlAsync(id, cancellationToken);
        return File(xml.Content, "application/xml", xml.FileName);
    }
}
