using System.Globalization;
using System.Numerics;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Ubl;

/// <summary>What the receiver of a commercial invoice says of it.</summary>
public enum UblAnswerKind
{
    Accepted = 1,

    Rejected = 2,
}

/// <summary>
/// The answer of the receiver to a commercial invoice (<c>TICARIFATURA</c>): a UBL-TR application response (UBL 2.1, customization <c>TR1.2</c>) from the receiver (the sender party) to the supplier (the receiver party) that says <c>KABUL</c> or <c>RED</c> of an invoice, named by its number
/// (and its uuid, in the reference of the response). It travels in an envelope of its own (<c>POSTBOXENVELOPE</c>) from the post box of the receiver to the sender unit of the supplier, signed like an invoice.
/// </summary>
public sealed record UblInvoiceAnswer(
    string Number,
    Guid Uuid,
    DateTime IssuedAt,
    UblAnswerKind Kind,
    string? Note,
    UblParty Sender,
    UblParty Receiver,
    string InvoiceNumber,
    Guid InvoiceUuid,
    DateTime InvoiceIssueDate)
{
    public const string AcceptedCode = "KABUL";
    public const string RejectedCode = "RED";
    public const string ProfileId = "TICARIFATURA";

    private static readonly XNamespace s_root = UblDocumentNames.ApplicationResponseNamespace;
    private static readonly XNamespace s_cbc = UblDocumentNames.Cbc;
    private static readonly XNamespace s_cac = UblDocumentNames.Cac;
    private static readonly XNamespace s_ext = UblDocumentNames.Ext;
    private static readonly XNamespace s_placeholder = "urn:edx:unsigned";

    /// <summary>A number for an answer in the form of the numbers of the e-invoice (three letters, the year, nine digits), made of its uuid.</summary>
    public static string NumberFor(Guid uuid, DateTime at)
        => $"CVP{at.Year.ToString(CultureInfo.InvariantCulture)}{(new BigInteger(uuid.ToByteArray(), isUnsigned: true) % 1_000_000_000).ToString("D9", CultureInfo.InvariantCulture)}";

    /// <summary>The unsigned application response (the signer puts the signature in the extension).</summary>
    public XElement ToElement()
    {
        string code = Kind == UblAnswerKind.Accepted ? AcceptedCode : RejectedCode;
        string description = Kind == UblAnswerKind.Accepted ? "FATURAKABUL" : "FATURARED";
        return new XElement(
            s_root + "ApplicationResponse",
            new XAttribute(XNamespace.Xmlns + "cbc", s_cbc),
            new XAttribute(XNamespace.Xmlns + "cac", s_cac),
            new XAttribute(XNamespace.Xmlns + "ext", s_ext),
            new XElement(s_ext + "UBLExtensions", new XElement(s_ext + "UBLExtension", new XElement(s_ext + "ExtensionContent", new XElement(s_placeholder + "Unsigned")))),
            new XElement(s_cbc + "UBLVersionID", "2.1"),
            new XElement(s_cbc + "CustomizationID", "TR1.2"),
            new XElement(s_cbc + "ProfileID", ProfileId),
            new XElement(s_cbc + "ID", Number),
            new XElement(s_cbc + "UUID", Uuid.ToString().ToUpperInvariant()),
            new XElement(s_cbc + "IssueDate", IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(s_cbc + "IssueTime", IssuedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
            string.IsNullOrWhiteSpace(Note) ? null : new XElement(s_cbc + "Note", Note),
            PartyElement("SenderParty", Sender),
            PartyElement("ReceiverParty", Receiver),
            new XElement(
                s_cac + "DocumentResponse",
                new XElement(
                    s_cac + "Response",
                    new XElement(s_cbc + "ReferenceID", InvoiceUuid.ToString().ToUpperInvariant()),
                    new XElement(s_cbc + "ResponseCode", code),
                    new XElement(s_cbc + "Description", description)),
                new XElement(
                    s_cac + "DocumentReference",
                    new XElement(s_cbc + "ID", InvoiceNumber),
                    new XElement(s_cbc + "IssueDate", InvoiceIssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement(s_cbc + "DocumentTypeCode", "FATURA"),
                    new XElement(s_cbc + "DocumentType", "FATURA"))));
    }

    /// <summary>Reads an application response that answers an invoice.</summary>
    /// <exception cref="UblInvoiceException">It is not one (a part is missing, the code is neither <c>KABUL</c> nor <c>RED</c>).</exception>
    public static UblInvoiceAnswer Read(XElement answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        try
        {
            XElement response = answer.Element(s_cac + "DocumentResponse")!;
            XElement result = response.Element(s_cac + "Response")!;
            XElement reference = response.Element(s_cac + "DocumentReference")!;
            string code = result.Element(s_cbc + "ResponseCode")!.Value.Trim();
            UblAnswerKind kind = code switch
            {
                AcceptedCode => UblAnswerKind.Accepted,
                RejectedCode => UblAnswerKind.Rejected,
                _ => throw new UblInvoiceException($"The response code '{code}' is neither {AcceptedCode} nor {RejectedCode}."),
            };
            DateTime issued = DateTime.ParseExact(
                $"{answer.Element(s_cbc + "IssueDate")!.Value.Trim()} {answer.Element(s_cbc + "IssueTime")?.Value.Trim() ?? "00:00:00"}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return new UblInvoiceAnswer(
                answer.Element(s_cbc + "ID")!.Value.Trim(),
                Guid.Parse(answer.Element(s_cbc + "UUID")!.Value.Trim()),
                issued,
                kind,
                answer.Element(s_cbc + "Note")?.Value.Trim(),
                ReadParty(answer.Element(s_cac + "SenderParty")!),
                ReadParty(answer.Element(s_cac + "ReceiverParty")!),
                reference.Element(s_cbc + "ID")!.Value.Trim(),
                Guid.TryParse(result.Element(s_cbc + "ReferenceID")?.Value.Trim(), out Guid invoiceUuid) ? invoiceUuid : Guid.Empty,
                DateTime.ParseExact(reference.Element(s_cbc + "IssueDate")!.Value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (exception is NullReferenceException or FormatException or OverflowException)
        {
            throw new UblInvoiceException("The document is not an application response to an invoice.");
        }
    }

    private static XElement PartyElement(string name, UblParty party)
        => new(
            s_cac + name,
            new XElement(s_cac + "PartyIdentification", new XElement(s_cbc + "ID", new XAttribute("schemeID", party.SchemeId), party.RegisterNumber)),
            new XElement(s_cac + "PartyName", new XElement(s_cbc + "Name", party.Name)),
            new XElement(
                s_cac + "PostalAddress",
                new XElement(s_cbc + "CitySubdivisionName", party.District),
                new XElement(s_cbc + "CityName", party.City),
                new XElement(s_cac + "Country", new XElement(s_cbc + "Name", party.Country))),
            string.IsNullOrWhiteSpace(party.TaxOffice) ? null : new XElement(s_cac + "PartyTaxScheme", new XElement(s_cac + "TaxScheme", new XElement(s_cbc + "Name", party.TaxOffice))));

    private static UblParty ReadParty(XElement party)
        => new(
            party.Element(s_cac + "PartyIdentification")!.Element(s_cbc + "ID")!.Value.Trim(),
            party.Element(s_cac + "PartyName")?.Element(s_cbc + "Name")?.Value.Trim() ?? string.Empty,
            party.Element(s_cac + "PartyTaxScheme")?.Element(s_cac + "TaxScheme")?.Element(s_cbc + "Name")?.Value.Trim(),
            party.Element(s_cac + "PostalAddress")?.Element(s_cbc + "CitySubdivisionName")?.Value.Trim() ?? string.Empty,
            party.Element(s_cac + "PostalAddress")?.Element(s_cbc + "CityName")?.Value.Trim() ?? string.Empty,
            party.Element(s_cac + "PostalAddress")?.Element(s_cac + "Country")?.Element(s_cbc + "Name")?.Value.Trim() ?? string.Empty);
}
