using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Envelope;

/// <summary>
/// Makes an envelope as Ek-1 of the guides says: a <c>StandardBusinessDocument</c> (the header with the sender, the receiver and the document identification, and the package <c>ef:Package</c> with <c>Elements</c>: <c>ElementType</c>, <c>ElementCount</c> and the <c>ElementList</c> of
/// documents), and the zip with one file, named by the uuid of the envelope, that is what is sent.
/// </summary>
public static class EnvelopeBuilder
{
    public const string HeaderNamespace = "http://www.unece.org/cefact/namespaces/StandardBusinessDocumentHeader";
    public const string PackageNamespace = "http://www.efatura.gov.tr/package-namespace";

    private static readonly XNamespace s_sh = HeaderNamespace;
    private static readonly XNamespace s_ef = PackageNamespace;
    private static readonly XNamespace s_xsi = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>The XML of an envelope.</summary>
    /// <param name="header">The header; <see cref="EnvelopeHeader.ElementCount"/> is written as the number of documents given.</param>
    /// <param name="documents">The documents: the root element of each.</param>
    public static byte[] BuildXml(EnvelopeHeader header, IReadOnlyList<XElement> documents)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(documents);

        XDocument document = new(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(
                s_sh + "StandardBusinessDocument",
                new XAttribute(XNamespace.Xmlns + "sh", HeaderNamespace),
                new XAttribute(XNamespace.Xmlns + "xsi", s_xsi.NamespaceName),
                new XAttribute(s_xsi + "schemaLocation", $"{HeaderNamespace} PackageProxy_1_2.xsd"),
                new XElement(
                    s_sh + "StandardBusinessDocumentHeader",
                    new XElement(s_sh + "HeaderVersion", header.HeaderVersion),
                    Party("Sender", header.Sender),
                    Party("Receiver", header.Receiver),
                    new XElement(
                        s_sh + "DocumentIdentification",
                        new XElement(s_sh + "Standard", header.Standard),
                        new XElement(s_sh + "TypeVersion", header.TypeVersion),
                        new XElement(s_sh + "InstanceIdentifier", header.InstanceIdentifier),
                        new XElement(s_sh + "Type", header.DocumentType),
                        new XElement(s_sh + "CreationDateAndTime", header.CreatedAt.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture)))),
                new XElement(
                    s_ef + "Package",
                    new XAttribute(XNamespace.Xmlns + "ef", PackageNamespace),
                    new XElement(
                        "Elements",
                        new XElement("ElementType", header.ElementType),
                        new XElement("ElementCount", documents.Count.ToString(CultureInfo.InvariantCulture)),
                        new XElement("ElementList", documents)))));

        using MemoryStream stream = new();
        using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = false, Encoding = new UTF8Encoding(false) }))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }

    /// <summary>The zip of an envelope: one file, <c>{envelope uuid}.xml</c>.</summary>
    public static byte[] Zip(string envelopeId, byte[] xml)
    {
        ArgumentException.ThrowIfNullOrEmpty(envelopeId);
        ArgumentNullException.ThrowIfNull(xml);

        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry entry = archive.CreateEntry(envelopeId + ".xml", CompressionLevel.Optimal);
            using Stream entryStream = entry.Open();
            entryStream.Write(xml);
        }

        return stream.ToArray();
    }

    /// <summary>The envelope of an invoice (its signed XML) from a sender unit to a post box, ready to send: its uuid and the zip. The invoice is not changed.</summary>
    public static (string EnvelopeId, byte[] Zip) BuildInvoiceEnvelope(EnvelopeParty sender, EnvelopeParty receiver, XElement invoice, DateTime createdAt, string? envelopeId = null)
    {
        string id = envelopeId ?? Guid.NewGuid().ToString();
        EnvelopeHeader header = EnvelopeHeader.Create(sender, receiver, EnvelopeTypes.Sender, id, createdAt, ElementTypes.Invoice, 1);
        return (id, Zip(id, BuildXml(header, [invoice])));
    }

    /// <summary>The envelope of the answer of a post box to a commercial invoice (<c>POSTBOXENVELOPE</c>, one <c>APPLICATIONRESPONSE</c>): from the post box of the receiver of the invoice to the sender unit of its supplier, ready to send.</summary>
    public static (string EnvelopeId, byte[] Zip) BuildAnswerEnvelope(EnvelopeParty fromPostBox, EnvelopeParty toSenderUnit, XElement answer, DateTime createdAt, string? envelopeId = null)
    {
        string id = envelopeId ?? Guid.NewGuid().ToString();
        EnvelopeHeader header = EnvelopeHeader.Create(fromPostBox, toSenderUnit, EnvelopeTypes.PostBox, id, createdAt, ElementTypes.ApplicationResponse, 1);
        return (id, Zip(id, BuildXml(header, [answer])));
    }

    private static XElement Party(string name, EnvelopeParty party)
        => new(
            s_sh + name,
            new XElement(s_sh + "Identifier", party.Alias),
            new XElement(s_sh + "ContactInformation", new XElement(s_sh + "Contact", party.RegisterNumber), new XElement(s_sh + "ContactTypeIdentifier", "VKN_TCKN")),
            new XElement(s_sh + "ContactInformation", new XElement(s_sh + "Contact", party.Title), new XElement(s_sh + "ContactTypeIdentifier", "UNVAN")));
}
