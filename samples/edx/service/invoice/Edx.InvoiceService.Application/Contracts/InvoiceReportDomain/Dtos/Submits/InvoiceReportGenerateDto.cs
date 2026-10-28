using System.ComponentModel.DataAnnotations;

namespace Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Dtos.Submits;

public sealed class InvoiceReportGenerateDto
{
    [Required]
    public DateTime? PeriodStart { get; set; }

    [Required]
    public DateTime? PeriodEnd { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "TRY";
}
