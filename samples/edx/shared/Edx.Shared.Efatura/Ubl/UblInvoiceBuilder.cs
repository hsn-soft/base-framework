using System.Globalization;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Ubl;

/// <summary>
/// Makes the UBL-TR 1.2.1 invoice (UBL 2.1, customization <c>TR1.2</c>, profile <c>TEMELFATURA</c>, type <c>SATIS</c>) of an <see cref="UblInvoice"/>: the elements the schema requires, in the order it says, with the amounts worked out the way the schematron checks them (each line's amount is its quantity
/// times its price, its tax is that amount times its rate, both to two decimals; the totals are their sums, and the taxes are subtotalled by rate). The document is unsigned: its extension holds a placeholder where the signature goes (<see cref="UblSignature"/>).
/// </summary>
public static class UblInvoiceBuilder
{
    /// <summary>The basic scenario (<c>TEMELFATURA</c>): the invoice only has to reach the receiver. It is the default profile.</summary>
    public const string ProfileId = "TEMELFATURA";

    /// <summary>The commercial scenario (<c>TICARIFATURA</c>): the receiver answers the invoice (accepts or refuses it) with an application response.</summary>
    public const string CommercialProfileId = "TICARIFATURA";

    public static bool IsKnownProfile(string? profile) => profile is ProfileId or CommercialProfileId;
    public const string InvoiceTypeCode = "SATIS";

    private static readonly XNamespace s_root = UblDocumentNames.InvoiceNamespace;
    private static readonly XNamespace s_cbc = UblDocumentNames.Cbc;
    private static readonly XNamespace s_cac = UblDocumentNames.Cac;
    private static readonly XNamespace s_ext = UblDocumentNames.Ext;
    private static readonly XNamespace s_placeholder = "urn:edx:unsigned";

    public static XElement Build(UblInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (invoice.Lines.Count == 0)
        {
            throw new ArgumentException("An invoice has at least one line.", nameof(invoice));
        }

        string currency = invoice.Currency.ToUpperInvariant();
        List<(UblLine Line, decimal Amount, decimal Tax)> lines = [.. invoice.Lines.Select(line =>
        {
            decimal amount = Money(line.Quantity * line.UnitPrice);
            return (line, amount, Money(amount * line.VatRate / 100m));
        })];
        decimal lineExtension = lines.Sum(item => item.Amount);
        decimal tax = lines.Sum(item => item.Tax);

        return new XElement(
            s_root + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", s_cbc),
            new XAttribute(XNamespace.Xmlns + "cac", s_cac),
            new XAttribute(XNamespace.Xmlns + "ext", s_ext),
            new XElement(s_ext + "UBLExtensions", new XElement(s_ext + "UBLExtension", new XElement(s_ext + "ExtensionContent", new XElement(s_placeholder + "Unsigned")))),
            new XElement(s_cbc + "UBLVersionID", "2.1"),
            new XElement(s_cbc + "CustomizationID", "TR1.2"),
            new XElement(s_cbc + "ProfileID", invoice.Profile),
            new XElement(s_cbc + "ID", invoice.Number),
            new XElement(s_cbc + "CopyIndicator", "false"),
            new XElement(s_cbc + "UUID", invoice.Uuid.ToString().ToUpperInvariant()),
            new XElement(s_cbc + "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(s_cbc + "IssueTime", "00:00:00"),
            new XElement(s_cbc + "InvoiceTypeCode", InvoiceTypeCode),
            new XElement(s_cbc + "DocumentCurrencyCode", currency),
            new XElement(s_cbc + "LineCountNumeric", lines.Count.ToString(CultureInfo.InvariantCulture)),
            SignatureBlock(invoice.Supplier),
            new XElement(s_cac + "AccountingSupplierParty", Party(invoice.Supplier)),
            new XElement(s_cac + "AccountingCustomerParty", Party(invoice.Customer)),
            TaxTotal(currency, tax, [.. lines.GroupBy(item => item.Line.VatRate).OrderBy(group => group.Key).Select(group => (Rate: group.Key, Taxable: group.Sum(item => item.Amount), Tax: group.Sum(item => item.Tax)))]),
            new XElement(
                s_cac + "LegalMonetaryTotal",
                Amount("LineExtensionAmount", currency, lineExtension),
                Amount("TaxExclusiveAmount", currency, lineExtension),
                Amount("TaxInclusiveAmount", currency, lineExtension + tax),
                Amount("PayableAmount", currency, lineExtension + tax)),
            lines.Select((item, index) => new XElement(
                s_cac + "InvoiceLine",
                new XElement(s_cbc + "ID", (index + 1).ToString(CultureInfo.InvariantCulture)),
                new XElement(s_cbc + "InvoicedQuantity", new XAttribute("unitCode", item.Line.UnitCode), Quantity(item.Line.Quantity)),
                Amount("LineExtensionAmount", currency, item.Amount),
                TaxTotal(currency, item.Tax, [(item.Line.VatRate, item.Amount, item.Tax)]),
                new XElement(s_cac + "Item", new XElement(s_cbc + "Name", item.Line.Description)),
                new XElement(s_cac + "Price", Amount("PriceAmount", currency, item.Line.UnitPrice)))));
    }

    // the signature the invoice says it has (who signs it, and where the signature is: <ds:Signature Id="Signature"> of the extension)
    private static XElement SignatureBlock(UblParty supplier)
        => new(
            s_cac + "Signature",
            new XElement(s_cbc + "ID", new XAttribute("schemeID", "VKN_TCKN"), supplier.RegisterNumber),
            new XElement(s_cac + "SignatoryParty", Identification(supplier), Address(supplier)),
            new XElement(s_cac + "DigitalSignatureAttachment", new XElement(s_cac + "ExternalReference", new XElement(s_cbc + "URI", "#Signature"))));

    private static XElement Party(UblParty party)
    {
        XElement result = new(
            s_cac + "Party",
            Identification(party),
            new XElement(s_cac + "PartyName", new XElement(s_cbc + "Name", party.Name)),
            Address(party));
        if (!string.IsNullOrWhiteSpace(party.TaxOffice))
        {
            result.Add(new XElement(s_cac + "PartyTaxScheme", new XElement(s_cac + "TaxScheme", new XElement(s_cbc + "Name", party.TaxOffice))));
        }

        return result;
    }

    private static XElement Identification(UblParty party)
        => new(s_cac + "PartyIdentification", new XElement(s_cbc + "ID", new XAttribute("schemeID", party.SchemeId), party.RegisterNumber));

    private static XElement Address(UblParty party)
        => new(
            s_cac + "PostalAddress",
            new XElement(s_cbc + "CitySubdivisionName", party.District),
            new XElement(s_cbc + "CityName", party.City),
            new XElement(s_cac + "Country", new XElement(s_cbc + "Name", party.Country)));

    private static XElement TaxTotal(string currency, decimal tax, IEnumerable<(decimal Rate, decimal Taxable, decimal Tax)> subtotals)
        => new(
            s_cac + "TaxTotal",
            Amount("TaxAmount", currency, tax),
            subtotals.Select(subtotal => new XElement(
                s_cac + "TaxSubtotal",
                Amount("TaxableAmount", currency, subtotal.Taxable),
                Amount("TaxAmount", currency, subtotal.Tax),
                new XElement(s_cbc + "Percent", Number(subtotal.Rate)),
                new XElement(s_cac + "TaxCategory", new XElement(s_cac + "TaxScheme", new XElement(s_cbc + "Name", "KDV"), new XElement(s_cbc + "TaxTypeCode", "0015"))))));

    private static XElement Amount(string name, string currency, decimal value)
        => new(s_cbc + name, new XAttribute("currencyID", currency), value.ToString("0.00", CultureInfo.InvariantCulture));

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Quantity(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
