using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos.Submits;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Services;
using Edx.InvoiceService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.InvoiceService.Http.Host.Controllers;

/// <summary>The reports of the invoices of a tenant. Reading is an endpoint permission, making and deleting are the buttons of the page (actions); how far back a caller looks is its constraint, and what a report amounts to is an operation.</summary>
[Route("api/invoice-service/v1/commercial/invoice-reports")]
public sealed class InvoiceReportsController(IInvoiceReportAppService reports) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.InvoiceReports.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<InvoiceReportDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => reports.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.InvoiceReports.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<InvoiceReportDto>> GetPagedListAsync([FromBody] GetInvoiceReportsPaged pagedInput, CancellationToken cancellationToken = default)
        => reports.GetPagedListAsync(pagedInput, cancellationToken);

    /// <summary>Makes a report of the invoices of a period: the period may not be longer than the days the caller may look back.</summary>
    [HttpPost("generate")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.InvoiceReports.Generate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateAsync([FromBody] InvoiceReportGenerateDto input, CancellationToken cancellationToken = default)
    {
        InvoiceReportDto created = await reports.GenerateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.InvoiceReports.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await reports.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
