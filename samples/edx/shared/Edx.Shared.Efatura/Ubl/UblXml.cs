using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Ubl;

/// <summary>Reads and writes the XML of a UBL document the way the center wants it: UTF-8, no DTD, no external entity.</summary>
public static class UblXml
{
    public static byte[] ToBytes(XElement document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using MemoryStream stream = new();
        using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
        {
            new XDocument(new XDeclaration("1.0", "UTF-8", null), document).Save(writer);
        }

        return stream.ToArray();
    }

    /// <exception cref="XmlException">The bytes are not well formed XML.</exception>
    public static XElement Parse(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        using MemoryStream stream = new(bytes, writable: false);
        using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XDocument.Load(reader).Root ?? throw new XmlException("The XML has no root element.");
    }
}
