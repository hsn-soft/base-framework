using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Edx.Shared.Contracts.Invoicing;

/// <summary>
/// The invoice as a customer says it: the model sent through the shared API (REST with JSON, gRPC, or WCF), and what every table form (a CSV, a text file, a workbook) is read into. The supplier is not in it: an invoice is issued by the tenant that sends it (its VKN, title and address are the tenant's
/// e-invoice profile), and the invoice service makes the UBL-TR document of the invoice and the tenant. It is one class for all of the forms: JSON by name, WCF by <c>[DataContract]</c> (its XML is the one a WCF client puts on the wire), and the gRPC contract is made of the same fields.
/// </summary>
[DataContract(Name = "InvoiceSubmission", Namespace = Namespace)]
public sealed class InvoiceSubmissionDto : IValidatableObject
{
    public const string Namespace = "urn:edx:firm-invoice:invoice:v2";

    /// <summary>The numbering of the e-invoice: three letters or digits (the series), the year and nine digits of a sequence, 16 characters (<c>ABC2026000000001</c>).</summary>
    public const string NumberPattern = "^[A-Za-z0-9]{3}20[0-9]{2}[0-9]{9}$";

    /// <summary>The id of the invoice (a uuid the customer makes: the ETTN of the e-invoice): the same invoice has the same id however many times it is sent, and it is what the invoice is found by.</summary>
    [DataMember(Order = 0, IsRequired = true)]
    public Guid InvoiceId { get; set; }

    /// <summary>The number of the invoice, in the numbering of the e-invoice (<see cref="NumberPattern"/>); its year is the year of the issue date.</summary>
    [DataMember(Order = 1)]
    [Required]
    [RegularExpression(NumberPattern, ErrorMessage = "The invoice number is 16 characters: three letters or digits, the year and nine digits (ABC2026000000001).")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public DateTime IssueDate { get; set; }

    /// <summary>The currency of the amounts: <c>TRY</c> (an invoice in another currency needs an exchange rate, which the model does not have yet).</summary>
    [DataMember(Order = 3)]
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>The customer: the title (or the name of the person) of the one the invoice is for.</summary>
    [DataMember(Order = 4)]
    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>The VKN (10 digits) or the TCKN (11 digits) of the customer: it is what the receiver's post box is found by.</summary>
    [DataMember(Order = 5)]
    [Required]
    [RegularExpression("^[0-9]{10,11}$", ErrorMessage = "The customer tax id is a VKN (10 digits) or a TCKN (11 digits).")]
    public string CustomerTaxId { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    [StringLength(100)]
    public string? CustomerTaxOffice { get; set; }

    [DataMember(Order = 7)]
    [Required]
    [StringLength(100)]
    public string CustomerDistrict { get; set; } = string.Empty;

    [DataMember(Order = 8)]
    [Required]
    [StringLength(100)]
    public string CustomerCity { get; set; } = string.Empty;

    /// <summary>The country of the customer; <c>Türkiye</c> when it is not given.</summary>
    [DataMember(Order = 9)]
    [StringLength(100)]
    public string? CustomerCountry { get; set; }

    [DataMember(Order = 10)]
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public List<InvoiceLineDto> Lines { get; set; } = [];

    /// <summary>The scenario: <c>TEMELFATURA</c> (the default: the invoice only has to reach the receiver) or <c>TICARIFATURA</c> (the receiver answers it, accepts or refuses).</summary>
    [DataMember(Order = 11)]
    [StringLength(16)]
    public string InvoiceProfile { get; set; } = "TEMELFATURA";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InvoiceId == Guid.Empty)
        {
            yield return new ValidationResult("The invoice id is required.", [nameof(InvoiceId)]);
        }

        if (IssueDate == default)
        {
            yield return new ValidationResult("The issue date is required.", [nameof(IssueDate)]);
        }
        else if (InvoiceNumber.Length == 16 && InvoiceNumber[3..7] != IssueDate.Year.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            yield return new ValidationResult("The year in the invoice number is not the year of the issue date.", [nameof(InvoiceNumber)]);
        }

        if (!string.Equals(InvoiceProfile, "TEMELFATURA", StringComparison.OrdinalIgnoreCase) && !string.Equals(InvoiceProfile, "TICARIFATURA", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("The invoice profile is TEMELFATURA or TICARIFATURA.", [nameof(InvoiceProfile)]);
        }

        if (!string.Equals(Currency, "TRY", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Only invoices in TRY are taken.", [nameof(Currency)]);
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

    /// <summary>The unit of the quantity, a UN/ECE code (<c>C62</c> a piece, <c>KGM</c> a kilogram, <c>MTR</c> a metre, <c>LTR</c> a litre, <c>HUR</c> an hour); <c>C62</c> when it is not given.</summary>
    [DataMember(Order = 3)]
    [RegularExpression("^[A-Z0-9]{2,3}$", ErrorMessage = "The unit is a UN/ECE code of two or three capital letters or digits (C62).")]
    public string? UnitCode { get; set; }

    /// <summary>The price of one unit, without VAT.</summary>
    [DataMember(Order = 4)]
    [Range(typeof(decimal), "0", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }

    /// <summary>The VAT rate in percent (<c>20</c>).</summary>
    [DataMember(Order = 5)]
    [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal VatRate { get; set; }
}
