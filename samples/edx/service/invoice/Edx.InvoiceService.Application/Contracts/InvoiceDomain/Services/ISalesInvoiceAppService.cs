using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.InvoiceDomain.Services;

/// <summary>
/// The invoices of a tenant, from the draft on. The endpoints ask for the permission of the action (<c>endpoint.invoice.*</c>); inside, the service asks the permissions that only it knows the need of: what the caller may see of an invoice (operations), how much it may
/// see and approve (constraints, which a caller must have been given). Invoices are not made here: they come from the queue (see <c>InvoiceImportService</c>).
/// </summary>
public interface ISalesInvoiceAppService
{
    Task<SalesInvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<SalesInvoiceDto>> GetPagedListAsync(GetSalesInvoicesPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>Approves a draft: it is signed afterwards (an event of the service itself).</summary>
    Task ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Rejects a draft, with the reason the sender reads; the sender may send the invoice again.</summary>
    Task RejectAsync(Guid id, InvoiceRejectDto input, CancellationToken cancellationToken = default);

    /// <summary>The XML of the invoice (the signed one, else the draft). It holds what the operations hide, so the caller must have both of them.</summary>
    Task<InvoiceXmlDto> GetXmlAsync(Guid id, CancellationToken cancellationToken = default);
}
