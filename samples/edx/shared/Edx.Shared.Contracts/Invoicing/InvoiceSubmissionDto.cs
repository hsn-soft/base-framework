using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Edx.Shared.Contracts.Invoicing;

/// <summary>
/// The invoice as the system knows it: the model a customer sends through the shared API (REST with JSON, gRPC, or WCF), and what every other form (a CSV, a text file, a workbook, an XML file) is read into. It is one class for all of them: JSON by name, WCF by
/// <c>[DataContract]</c> (its XML is the one a WCF client puts on the wire, and the one the draft of an invoice is), and the gRPC contract is made of the same fields.
/// </summary>
[DataContract(Name = "InvoiceSubmission", Namespace = Namespace)]
public sealed class InvoiceSubmissionDto : IValidatableObject
{
    public const string Namespace = "urn:edx:intake:invoice:v1";

    /// <summary>The id of the invoice (a uuid the customer makes): the same invoice has the same id however many times it is sent, and it is what the invoice is found by.</summary>
    [DataMember(Order = 0, IsRequired = true)]
    public Guid InvoiceId { get; set; }

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InvoiceId == Guid.Empty)
        {
            yield return new ValidationResult("The invoice id is required.", [nameof(InvoiceId)]);
        }

        for (int index = 0; index < Lines.Count; index++)
        {
            List<ValidationResult> problems = [];
            Validator.TryValidateObject(Lines[index], new ValidationContext(Lines[index]), problems, validateAllProperties: true);
            foreach (ValidationResult problem in problems)
            {
                yield return new ValidationResult($"Lines[{index}]: {problem.ErrorMessage}", problem.MemberNames);
            }
        }
    }
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
