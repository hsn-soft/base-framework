using System.ComponentModel.DataAnnotations;
using Edx.Shared.Contracts.Invoicing;

namespace Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos.Submits;

/// <summary>What the sender says of the zip it sends: which invoice it is (the uuid and the number the sender gave it), the SHA-256 of the zip (the service computes it too and refuses the file when they differ) and the type of the one file inside.</summary>
public sealed class InvoiceUploadDto
{
    [Required]
    public Guid? InvoiceId { get; set; }

    [Required]
    [StringLength(64)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>The SHA-256 of the zip, in hex (64 characters).</summary>
    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary><c>Xml</c>, <c>Csv</c>, <c>Txt</c> or <c>Xls</c>: the type of the file in the zip.</summary>
    [Required]
    public InvoiceSourceType? SourceType { get; set; }

    /// <summary>The name of the zip.</summary>
    [Required]
    [StringLength(200)]
    public string FileName { get; set; } = string.Empty;
}
