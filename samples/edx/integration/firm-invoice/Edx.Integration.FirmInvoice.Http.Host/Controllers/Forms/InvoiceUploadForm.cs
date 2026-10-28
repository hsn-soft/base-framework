using System.ComponentModel.DataAnnotations;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos.Submits;
using Edx.Shared.Contracts.Invoicing;

namespace Edx.Integration.FirmInvoice.Http.Host.Controllers.Forms;

/// <summary>The form of an upload (multipart): the zip, and what the sender says of it. The same for the API of the customers and for the upload page of the back office.</summary>
public sealed class InvoiceUploadForm
{
    /// <summary>The zip, with one file in it.</summary>
    [Required]
    public IFormFile? File { get; set; }

    /// <summary>The uuid of the invoice.</summary>
    [Required]
    public Guid? InvoiceId { get; set; }

    [Required]
    [StringLength(64)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>The SHA-256 of the zip, in hex.</summary>
    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary><c>Xml</c>, <c>Csv</c>, <c>Txt</c> or <c>Xls</c>: the type of the file in the zip.</summary>
    [Required]
    public InvoiceSourceType? SourceType { get; set; }

    public InvoiceUploadDto ToDto() => new()
    {
        InvoiceId = InvoiceId,
        InvoiceNumber = InvoiceNumber,
        FileHash = FileHash,
        SourceType = SourceType,
        FileName = File?.FileName ?? string.Empty,
    };
}
