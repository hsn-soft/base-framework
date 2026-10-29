namespace Edx.Shared.Efatura.Ubl;

/// <summary>A party of an invoice (the supplier that issues it, the customer that receives it): who it is and where it is (the UBL-TR schema asks for the district, the city and the country of every party).</summary>
/// <param name="RegisterNumber">The VKN (10 digits) or the TCKN (11 digits).</param>
/// <param name="Name">The title (or the name of a person).</param>
/// <param name="TaxOffice">The tax office; none when it is not known.</param>
/// <param name="District">The district (<c>CitySubdivisionName</c>).</param>
/// <param name="City">The city (<c>CityName</c>).</param>
/// <param name="Country">The country.</param>
public sealed record UblParty(string RegisterNumber, string Name, string? TaxOffice, string District, string City, string Country)
{
    /// <summary><c>VKN</c> for a legal person (10 digits), <c>TCKN</c> for a person (11).</summary>
    public string SchemeId => RegisterNumber.Length == 11 ? "TCKN" : "VKN";
}

/// <summary>A line of an invoice: what was sold, how many (and in what unit: a UN/ECE code, <c>C62</c> for a piece), at what price without VAT and with what VAT rate in percent.</summary>
public sealed record UblLine(string Description, decimal Quantity, string UnitCode, decimal UnitPrice, decimal VatRate);

/// <summary>What an invoice (the basic scenario, a sale, in Turkish liras) is made of; <see cref="UblInvoiceBuilder"/> makes the UBL-TR document of it.</summary>
/// <param name="Number">The number: 16 characters, three of letters or digits, the year, nine digits of a sequence.</param>
/// <param name="Uuid">The universal unique number (ETTN).</param>
/// <param name="IssueDate">The date of the invoice.</param>
/// <param name="Currency">The currency (the builder does the amounts in one currency).</param>
/// <param name="Supplier">Who issues the invoice.</param>
/// <param name="Customer">Who receives it.</param>
/// <param name="Lines">The lines (at least one).</param>
/// <param name="Profile">The scenario: <c>TEMELFATURA</c> (the default) or <c>TICARIFATURA</c> (the receiver answers the invoice).</param>
public sealed record UblInvoice(string Number, Guid Uuid, DateTime IssueDate, string Currency, UblParty Supplier, UblParty Customer, IReadOnlyList<UblLine> Lines, string Profile = UblInvoiceBuilder.ProfileId);

/// <summary>A line as an invoice says it (read back): its amounts are the document's, rounded as the document rounded them.</summary>
public sealed record UblLineSummary(string Description, decimal Quantity, string UnitCode, decimal UnitPrice, decimal VatRate, decimal LineAmount, decimal TaxAmount);

/// <summary>What an invoice says of itself, read from its UBL-TR document: enough to list it, to limit it and to tell who it is from and for.</summary>
public sealed record UblInvoiceSummary(
    string Number,
    Guid Uuid,
    DateTime IssueDate,
    string ProfileId,
    string InvoiceTypeCode,
    string Currency,
    string SupplierRegisterNumber,
    string SupplierName,
    string? SupplierTaxOffice,
    string CustomerRegisterNumber,
    string CustomerName,
    string? CustomerTaxOffice,
    decimal LineExtensionAmount,
    decimal TaxExclusiveAmount,
    decimal TaxInclusiveAmount,
    decimal PayableAmount,
    IReadOnlyList<UblLineSummary> Lines);
