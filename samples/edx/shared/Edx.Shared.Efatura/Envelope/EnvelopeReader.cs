using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Envelope;

/// <summary>
/// Reads an envelope the way the center does: a zip with exactly one XML file, which is a StandardBusinessDocument of the header version 1.0 (standard UBL-TR, type version 1.2) with its sender, receiver, document identification and package. What is wrong is an <see cref="EnvelopeException"/> with the code of the center
/// (<c>1110</c> not a zip, <c>1131</c> not one file, <c>1132</c> not XML, <c>1140</c> not parsed, <c>1141</c> no uuid, <c>1111</c> the uuid is not 36 characters, <c>1133</c> the uuid and the XML name differ, <c>1143</c> a version, the standard).
/// </summary>
public static class EnvelopeReader
{
    private const long MaxUnpackedBytes = 20 * 1024 * 1024;

    /// <summary>The XML file inside the zip: its name and its bytes.</summary>
    public static (string Name, byte[] Xml) Unzip(byte[] zip)
    {
        ArgumentNullException.ThrowIfNull(zip);

        if (zip.Length == 0)
        {
            throw new EnvelopeException(EfaturaStatus.NotUnpacked);
        }

        try
        {
            using MemoryStream stream = new(zip, writable: false);
            using ZipArchive archive = new(stream, ZipArchiveMode.Read);
            ZipArchiveEntry[] files = [.. archive.Entries.Where(entry => !entry.FullName.EndsWith('/'))];
            if (files.Length != 1)
            {
                throw new EnvelopeException(EfaturaStatus.ZipMustHoldOneFile);
            }

            ZipArchiveEntry entry = files[0];
            if (!entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || entry.Length > MaxUnpackedBytes)
            {
                throw new EnvelopeException(EfaturaStatus.NotXml);
            }

            using Stream entryStream = entry.Open();
            using MemoryStream unpacked = new();
            entryStream.CopyTo(unpacked);
            return (entry.Name, unpacked.ToArray());
        }
        catch (InvalidDataException)
        {
            throw new EnvelopeException(EfaturaStatus.NotUnpacked);
        }
    }

    /// <summary>The header and the elements of the zip of an envelope.</summary>
    public static EnvelopeContent ReadZip(byte[] zip)
    {
        (string name, byte[] xml) = Unzip(zip);
        EnvelopeContent content = ReadXml(xml);
        if (content.Header.InstanceIdentifier.Length != 36)
        {
            throw new EnvelopeException(EfaturaStatus.EnvelopeIdLengthInvalid);
        }

        if (!string.Equals(Path.GetFileNameWithoutExtension(name), content.Header.InstanceIdentifier, StringComparison.Ordinal))
        {
            throw new EnvelopeException(EfaturaStatus.EnvelopeIdAndXmlNameDiffer);
        }

        return content;
    }

    /// <summary>The header and the documents of the XML of an envelope (no DTD, no external entity).</summary>
    public static EnvelopeContent ReadXml(byte[] xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        XDocument document;
        try
        {
            using MemoryStream stream = new(xml, writable: false);
            using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            document = XDocument.Load(reader);
        }
        catch (XmlException)
        {
            throw new EnvelopeException(EfaturaStatus.NotXml);
        }

        try
        {
            XElement root = document.Root is { Name.LocalName: "StandardBusinessDocument" } ? document.Root : throw new EnvelopeException(EfaturaStatus.DocumentNotParsed);
            XElement header = Child(root, "StandardBusinessDocumentHeader");
            XElement identification = Child(header, "DocumentIdentification");
            string instance = Child(identification, "InstanceIdentifier").Value.Trim();
            if (instance.Length == 0)
            {
                throw new EnvelopeException(EfaturaStatus.EnvelopeIdMissing);
            }

            string headerVersion = Child(header, "HeaderVersion").Value.Trim();
            string standard = Child(identification, "Standard").Value.Trim();
            string typeVersion = Child(identification, "TypeVersion").Value.Trim();
            if (headerVersion != EnvelopeHeader.CurrentHeaderVersion || standard != EnvelopeHeader.CurrentStandard || typeVersion != EnvelopeHeader.CurrentTypeVersion)
            {
                throw new EnvelopeException(EfaturaStatus.VersionInvalid);
            }

            XElement elements = Child(Child(root, "Package"), "Elements");
            List<XElement> documents = [.. Child(elements, "ElementList").Elements()];
            string? elementType = elements.Elements().FirstOrDefault(e => e.Name.LocalName == "ElementType")?.Value.Trim();
            int count = int.Parse(Child(elements, "ElementCount").Value.Trim(), CultureInfo.InvariantCulture);

            string created = Child(identification, "CreationDateAndTime").Value.Trim();
            DateTime createdAt = DateTime.ParseExact(created.Length >= 19 ? created[..19] : created, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

            return new EnvelopeContent(
                new EnvelopeHeader(headerVersion, ReadParty(Child(header, "Sender")), ReadParty(Child(header, "Receiver")), standard, typeVersion, instance, Child(identification, "Type").Value.Trim(), createdAt, elementType, count),
                documents);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or InvalidOperationException)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed);
        }
    }

    private static EnvelopeParty ReadParty(XElement party)
    {
        string alias = Child(party, "Identifier").Value.Trim();
        string registerNumber = string.Empty;
        string title = string.Empty;
        foreach (XElement contact in party.Elements().Where(e => e.Name.LocalName == "ContactInformation"))
        {
            string value = Child(contact, "Contact").Value.Trim();
            switch (Child(contact, "ContactTypeIdentifier").Value.Trim())
            {
                case "VKN_TCKN":
                    registerNumber = value;
                    break;
                case "UNVAN":
                    title = value;
                    break;
            }
        }

        return new EnvelopeParty(alias, registerNumber, title);
    }

    private static XElement Child(XElement parent, string localName)
        => parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName) ?? throw new EnvelopeException(EfaturaStatus.DocumentNotParsed);
}
