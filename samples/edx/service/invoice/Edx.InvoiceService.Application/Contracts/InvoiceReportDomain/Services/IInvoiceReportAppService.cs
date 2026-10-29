using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Services;

/// <summary>
/// The reports of the invoices of a tenant. The constraint <c>constraint.invoice-report.max-days</c> is how far back the caller may look: it sees only the reports made in the last days, and makes a report of a period no longer than that; a caller that was given none is
/// refused before anything is read. What a report amounts to is shown to who has <c>operation.invoice-report.total.view</c>.
/// </summary>
public interface IInvoiceReportAppService
{
    Task<InvoiceReportDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<InvoiceReportDto>> GetPagedListAsync(GetInvoiceReportsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<InvoiceReportDto> GenerateAsync(InvoiceReportGenerateDto input, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
