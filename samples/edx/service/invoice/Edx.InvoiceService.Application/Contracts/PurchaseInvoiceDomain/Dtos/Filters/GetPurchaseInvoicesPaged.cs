using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Filters;

public sealed class GetPurchaseInvoicesPaged : PagedDataRequestDto
{
    /// <summary>A part of the number.</summary>
    [StringLength(64)]
    public string? Number { get; set; }

    /// <summary>A part of the name of the one who issued the invoice.</summary>
    [StringLength(200)]
    public string? SupplierName { get; set; }
}
