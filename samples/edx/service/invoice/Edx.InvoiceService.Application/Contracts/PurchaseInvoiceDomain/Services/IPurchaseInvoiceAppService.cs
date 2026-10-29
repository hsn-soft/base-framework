using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Services;

/// <summary>The invoices issued to a tenant (they come in at its post box). The endpoints ask for the permission of the action (<c>endpoint.purchase-invoice.*</c>); inside, the service asks what only it knows: what the caller may see (operation) and how much (constraint).</summary>
public interface IPurchaseInvoiceAppService
{
    Task<PurchaseInvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<PurchaseInvoiceDto>> GetPagedListAsync(GetPurchaseInvoicesPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>The XML of the invoice as it came. It holds what the operations hide, so the caller must have both of them.</summary>
    Task<InvoiceXmlDto> GetXmlAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Answers a commercial invoice that came in (accepts or refuses it): the answer document is made, signed by the signer service, and sent to the supplier through the GİB invoice integration.</summary>
    Task<PurchaseInvoiceDto> RespondAsync(Guid id, PurchaseInvoiceRespondDto input, CancellationToken cancellationToken = default);
}
