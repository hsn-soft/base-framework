using System.Globalization;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Envelope;

/// <summary>
/// The answer about an envelope (Ek-2 of the guides): a UBL <c>ApplicationResponse</c> in a system envelope. It says who sent it and who it is for (<c>SenderParty</c>, <c>ReceiverParty</c>), which envelope it is about (<c>DocumentReference</c>: its uuid, its date, its kind) and the status
/// of that envelope (<c>LineResponse</c>: the code and the text of <see cref="EfaturaStatus"/>). It is what the center sends the sender when it has analyzed an envelope, what a post box sends back when it took one in, and what <c>getApplicationResponse</c> returns.
/// </summary>
/// <param name="ReferenceEnvelopeId">The uuid of the envelope the answer is about.</param>
/// <param name="ReferenceEnvelopeType">The kind of that envelope (<see cref="EnvelopeTypes"/>).</param>
/// <param name="ReferenceIssueDate">When that envelope was made.</param>
/// <param name="ResultCode">The status (<see cref="EfaturaStatus"/>).</param>
/// <param name="ResultDescription">The text of the status.</param>
/// <param name="IssuedAt">When the answer was made.</param>
/// <param name="Sender">Who answers (the alias it answers from, its register number and title).</param>
/// <param name="Receiver">Who the answer is for: the one the envelope came from.</param>
public sealed record SystemResponse(
    string ReferenceEnvelopeId,
    string ReferenceEnvelopeType,
    DateTime ReferenceIssueDate,
    int ResultCode,
    string ResultDescription,
    DateTime IssuedAt,
    EnvelopeParty Sender,
    EnvelopeParty Receiver)
{
    public const string ResponseCode = "S_APR";
    public const string ProfileId = "UBL-TR-PROFILE-1";

    private static readonly XNamespace s_root = "urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2";
    private static readonly XNamespace s_cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace s_cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    /// <summary>The <c>ApplicationResponse</c> element, with its own uuid.</summary>
    public XElement ToElement()
    {
        string id = Guid.NewGuid().ToString();
        string issueDate = ReferenceIssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new XElement(
            s_root + "ApplicationResponse",
            new XAttribute(XNamespace.Xmlns + "cbc", s_cbc),
            new XAttribute(XNamespace.Xmlns + "cac", s_cac),
            new XElement(s_cbc + "UBLVersionID", "2.1"),
            new XElement(s_cbc + "CustomizationID", "TR1.2"),
            new XElement(s_cbc + "ProfileID", ProfileId),
            new XElement(s_cbc + "ID", id),
            new XElement(s_cbc + "UUID", id),
            new XElement(s_cbc + "IssueDate", IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(s_cbc + "IssueTime", IssuedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
            Party("SenderParty", Sender),
            Party("ReceiverParty", Receiver),
            new XElement(
                s_cac + "DocumentResponse",
                new XElement(
                    s_cac + "Response",
                    new XElement(s_cbc + "ReferenceID", Guid.NewGuid().ToString()),
                    new XElement(s_cbc + "ResponseCode", ResponseCode),
                    new XElement(s_cbc + "Description", "APPLICATIONRESPONSE")),
                new XElement(
                    s_cac + "DocumentReference",
                    new XElement(s_cbc + "ID", ReferenceEnvelopeId),
                    new XElement(s_cbc + "IssueDate", issueDate),
                    new XElement(s_cbc + "DocumentTypeCode", ReferenceEnvelopeType),
                    new XElement(s_cbc + "DocumentType", ReferenceEnvelopeType)),
                new XElement(
                    s_cac + "LineResponse",
                    new XElement(
                        s_cac + "LineReference",
                        new XElement(s_cbc + "LineID", "0"),
                        new XElement(s_cac + "DocumentReference", new XElement(s_cbc + "ID", ReferenceEnvelopeId), new XElement(s_cbc + "IssueDate", issueDate))),
                    new XElement(
                        s_cac + "Response",
                        new XElement(s_cbc + "ReferenceID", Guid.NewGuid().ToString()),
                        new XElement(s_cbc + "ResponseCode", ResultCode.ToString(CultureInfo.InvariantCulture)),
                        new XElement(s_cbc + "Description", ResultDescription)))));
    }

    /// <summary>The system envelope (sent from <see cref="Sender"/> to <see cref="Receiver"/>): its uuid, its XML and its zip.</summary>
    public (string EnvelopeId, byte[] Xml, byte[] Zip) ToEnvelope()
    {
        string id = Guid.NewGuid().ToString();
        EnvelopeHeader header = EnvelopeHeader.Create(Sender, Receiver, EnvelopeTypes.System, id, IssuedAt, ElementTypes.ApplicationResponse, 1);
        byte[] xml = EnvelopeBuilder.BuildXml(header, [ToElement()]);
        return (id, xml, EnvelopeBuilder.Zip(id, xml));
    }

    /// <summary>Reads the answer from an <c>ApplicationResponse</c> element.</summary>
    /// <exception cref="EnvelopeException">The element is not an answer of this form.</exception>
    public static SystemResponse FromElement(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        try
        {
            XElement response = element.Element(s_cac + "DocumentResponse")!;
            XElement reference = response.Element(s_cac + "DocumentReference")!;
            XElement result = response.Element(s_cac + "LineResponse")!.Element(s_cac + "Response")!;
            DateTime issued = DateTime.ParseExact(
                $"{element.Element(s_cbc + "IssueDate")!.Value} {element.Element(s_cbc + "IssueTime")?.Value ?? "00:00:00"}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return new SystemResponse(
                reference.Element(s_cbc + "ID")!.Value,
                reference.Element(s_cbc + "DocumentTypeCode")?.Value ?? EnvelopeTypes.Sender,
                DateTime.ParseExact(reference.Element(s_cbc + "IssueDate")!.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                int.Parse(result.Element(s_cbc + "ResponseCode")!.Value, CultureInfo.InvariantCulture),
                result.Element(s_cbc + "Description")?.Value ?? string.Empty,
                issued,
                ReadParty(element.Element(s_cac + "SenderParty")!),
                ReadParty(element.Element(s_cac + "ReceiverParty")!));
        }
        catch (Exception exception) when (exception is NullReferenceException or FormatException or OverflowException)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed);
        }
    }

    // the party of a system response has no alias (the envelope header has it): the register number and the name. The UBL-TR schema wants an address (district, city, country) in every party; a system response of a mock does not know the real one.
    private static XElement Party(string name, EnvelopeParty party)
        => new(
            s_cac + name,
            new XElement(s_cac + "PartyIdentification", new XElement(s_cbc + "ID", new XAttribute("schemeID", party.RegisterNumber.Length == 10 ? "VKN" : "TCKN"), party.RegisterNumber)),
            new XElement(s_cac + "PartyName", new XElement(s_cbc + "Name", party.Title)),
            new XElement(
                s_cac + "PostalAddress",
                new XElement(s_cbc + "CitySubdivisionName", "-"),
                new XElement(s_cbc + "CityName", "-"),
                new XElement(s_cac + "Country", new XElement(s_cbc + "Name", "Türkiye"))));

    private static EnvelopeParty ReadParty(XElement party)
        => new(
            string.Empty,
            party.Element(s_cac + "PartyIdentification")!.Element(s_cbc + "ID")!.Value,
            party.Element(s_cac + "PartyName")?.Element(s_cbc + "Name")?.Value ?? string.Empty);
}
