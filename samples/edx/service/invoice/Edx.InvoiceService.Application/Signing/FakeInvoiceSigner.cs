using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Edx.InvoiceService.Application.Signing;

/// <summary>A signer that signs nothing: it adds a <c>Signature</c> element that says it is fake and holds the SHA-256 of the XML, so what the pipeline does with a signed invoice can be seen end to end.</summary>
public sealed class FakeInvoiceSigner(TimeProvider time) : IInvoiceSigner
{
    private static readonly XNamespace s_signature = "urn:edx:fake-signature:v1";

    public byte[] Sign(byte[] xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        using MemoryStream input = new(xml, writable: false);
        using XmlReader reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        XDocument document = XDocument.Load(reader);
        document.Root!.Add(new XElement(
            s_signature + "Signature",
            new XElement(s_signature + "Algorithm", "FAKE-SHA256"),
            new XElement(s_signature + "Value", Convert.ToHexStringLower(SHA256.HashData(xml))),
            new XElement(s_signature + "SignedAt", time.GetUtcNow().ToString("O", System.Globalization.CultureInfo.InvariantCulture))));

        using MemoryStream output = new();
        using (XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
        {
            document.Save(writer);
        }

        return output.ToArray();
    }
}
