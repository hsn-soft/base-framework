using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;

/// <summary>
/// The model a customer sends through the shared API (REST with JSON, gRPC, or WCF). It is one class for all of them: JSON by name, WCF by <c>[DataContract]</c> (the XML it is kept as is the same XML a WCF client
/// puts on the wire), and the gRPC contract is made of the same fields. The service keeps it as a JSON or an XML file.
/// </summary>
[DataContract(Name = "InvoiceSubmission", Namespace = Namespace)]
public sealed class InvoiceSubmissionDto
{
    public const string Namespace = "urn:edx:intake:invoice:v1";

    /// <summary>The number of the invoice, as the customer numbers them.</summary>
    [DataMember(Order = 1)]
    [Required]
    [StringLength(64)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public DateTime IssueDate { get; set; }

    /// <summary>The currency of the amounts, as an ISO 4217 code (<c>TRY</c>).</summary>
    [DataMember(Order = 3)]
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    [Required]
    [StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    [StringLength(32)]
    public string? SupplierTaxId { get; set; }

    [DataMember(Order = 6)]
    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [DataMember(Order = 7)]
    [StringLength(32)]
    public string? CustomerTaxId { get; set; }

    [DataMember(Order = 8)]
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public List<InvoiceLineDto> Lines { get; set; } = [];
}

/// <summary>One line of an invoice.</summary>
[DataContract(Name = "InvoiceLine", Namespace = InvoiceSubmissionDto.Namespace)]
public sealed class InvoiceLineDto
{
    [DataMember(Order = 1)]
    [Required]
    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    [Range(typeof(decimal), "0.0001", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Quantity { get; set; }

    [DataMember(Order = 3)]
    [Range(typeof(decimal), "0", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }

    /// <summary>The VAT rate in percent (<c>20</c>).</summary>
    [DataMember(Order = 4)]
    [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal VatRate { get; set; }
}
