using System.Globalization;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Ubl;

/// <summary>The invoice is not what the reader needs of it (a part is missing, a value is not a number): the message tells which part. A document that follows the schema does not do this.</summary>
public sealed class UblInvoiceException(string message) : Exception(message);

/// <summary>Reads what an invoice says of itself from its UBL-TR document (see <see cref="UblInvoiceSummary"/>).</summary>
public static class UblInvoiceReader
{
    private static readonly XNamespace s_cbc = UblDocumentNames.Cbc;
    private static readonly XNamespace s_cac = UblDocumentNames.Cac;

    public static UblInvoiceSummary Read(XElement invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        try
        {
            XElement supplier = Required(invoice, s_cac + "AccountingSupplierParty", "AccountingSupplierParty").Element(s_cac + "Party") ?? throw Missing("AccountingSupplierParty/Party");
            XElement customer = Required(invoice, s_cac + "AccountingCustomerParty", "AccountingCustomerParty").Element(s_cac + "Party") ?? throw Missing("AccountingCustomerParty/Party");
            XElement total = Required(invoice, s_cac + "LegalMonetaryTotal", "LegalMonetaryTotal");
            return new UblInvoiceSummary(
                Text(invoice, "ID"),
                Guid.Parse(Text(invoice, "UUID")),
                DateTime.ParseExact(Text(invoice, "IssueDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Text(invoice, "ProfileID"),
                Text(invoice, "InvoiceTypeCode"),
                Text(invoice, "DocumentCurrencyCode"),
                RegisterNumber(supplier),
                Name(supplier),
                TaxOffice(supplier),
                RegisterNumber(customer),
                Name(customer),
                TaxOffice(customer),
                Amount(total, "LineExtensionAmount"),
                Amount(total, "TaxExclusiveAmount"),
                Amount(total, "TaxInclusiveAmount"),
                Amount(total, "PayableAmount"),
                [.. invoice.Elements(s_cac + "InvoiceLine").Select(Line)]);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException)
        {
            throw new UblInvoiceException($"The invoice has a value that cannot be read: {exception.Message}");
        }
    }

    private static UblLineSummary Line(XElement line)
    {
        XElement? quantity = line.Element(s_cbc + "InvoicedQuantity");
        XElement? subtotal = line.Element(s_cac + "TaxTotal")?.Element(s_cac + "TaxSubtotal");
        return new UblLineSummary(
            line.Element(s_cac + "Item")?.Element(s_cbc + "Name")?.Value.Trim() ?? string.Empty,
            Number(quantity?.Value),
            quantity?.Attribute("unitCode")?.Value ?? string.Empty,
            Number(line.Element(s_cac + "Price")?.Element(s_cbc + "PriceAmount")?.Value),
            Number(subtotal?.Element(s_cbc + "Percent")?.Value),
            Amount(line, "LineExtensionAmount"),
            Number(line.Element(s_cac + "TaxTotal")?.Element(s_cbc + "TaxAmount")?.Value));
    }

    private static string RegisterNumber(XElement party)
        => party.Elements(s_cac + "PartyIdentification").Select(identification => identification.Element(s_cbc + "ID")).FirstOrDefault(id => id?.Attribute("schemeID")?.Value is "VKN" or "TCKN")?.Value.Trim() ?? throw Missing("PartyIdentification (VKN or TCKN)");

    private static string Name(XElement party) => party.Element(s_cac + "PartyName")?.Element(s_cbc + "Name")?.Value.Trim() ?? party.Element(s_cac + "Person")?.Element(s_cbc + "FirstName")?.Value.Trim() ?? string.Empty;

    private static string? TaxOffice(XElement party) => party.Element(s_cac + "PartyTaxScheme")?.Element(s_cac + "TaxScheme")?.Element(s_cbc + "Name")?.Value.Trim();

    private static XElement Required(XElement parent, XName name, string what) => parent.Element(name) ?? throw Missing(what);

    private static string Text(XElement parent, string localName) => parent.Element(s_cbc + localName)?.Value.Trim() is { Length: > 0 } text ? text : throw Missing(localName);

    private static decimal Amount(XElement parent, string localName) => Number(Text(parent, localName));

    private static decimal Number(string? text) => string.IsNullOrWhiteSpace(text) ? 0m : decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static UblInvoiceException Missing(string part) => new($"The invoice has no {part}.");
}
