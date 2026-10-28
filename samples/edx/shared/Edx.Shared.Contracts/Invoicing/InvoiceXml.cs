using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Edx.Shared.Contracts.Invoicing;

/// <summary>
/// The XML of an invoice: the data contract of <see cref="InvoiceSubmissionDto"/> (<c>urn:edx:firm-invoice:invoice:v1</c>), which is what a WCF client puts on the wire, what a model kept as XML is, what an XML file of an invoice has to be and what the draft of an invoice is. <see cref="Read"/> checks a document against
/// the schema of the contract (made from the contract itself, so the schema and the class cannot differ) before it reads it, so a document that does not follow the schema is told what is wrong with it.
/// </summary>
public static class InvoiceXml
{
    private static readonly Lazy<XmlSchemaSet> s_schemas = new(MakeSchemas);

    /// <summary>The XML of the invoice, indented, in UTF-8 without a byte order mark.</summary>
    public static byte[] Write(InvoiceSubmissionDto invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        using MemoryStream stream = new();
        using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
        {
            new DataContractSerializer(typeof(InvoiceSubmissionDto)).WriteObject(writer, invoice);
        }

        return stream.ToArray();
    }

    /// <summary>Reads the XML of an invoice: it has to be well formed and to follow the schema of the contract (no DTD, no external entity).</summary>
    /// <exception cref="InvoiceXmlException">What is wrong with the document.</exception>
    public static InvoiceSubmissionDto Read(byte[] xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        List<string> problems = [];
        XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, ValidationType = ValidationType.Schema, Schemas = s_schemas.Value };
        settings.ValidationEventHandler += (_, e) => problems.Add($"line {e.Exception?.LineNumber}: {e.Message}");
        try
        {
            using MemoryStream stream = new(xml);
            using XmlReader reader = XmlReader.Create(stream, settings);
            while (reader.Read())
            {
                // reading validates
            }
        }
        catch (XmlException exception)
        {
            throw new InvoiceXmlException($"The XML is not well formed: {exception.Message}");
        }

        if (problems.Count > 0)
        {
            throw new InvoiceXmlException($"The XML does not follow the schema of the invoice: {string.Join(" ", problems.Take(5))}");
        }

        try
        {
            using MemoryStream stream = new(xml);
            using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            return (InvoiceSubmissionDto?)new DataContractSerializer(typeof(InvoiceSubmissionDto)).ReadObject(reader) ?? throw new InvoiceXmlException("The XML holds no invoice.");
        }
        catch (SerializationException exception)
        {
            throw new InvoiceXmlException($"The XML cannot be read as an invoice: {exception.Message}");
        }
    }

    private static XmlSchemaSet MakeSchemas()
    {
        XsdDataContractExporter exporter = new();
        exporter.Export(typeof(InvoiceSubmissionDto));
        XmlSchemaSet schemas = new();
        foreach (XmlSchema schema in exporter.Schemas.Schemas())
        {
            schemas.Add(schema);
        }

        schemas.Compile();
        return schemas;
    }
}

/// <summary>The XML of an invoice is not what the contract says (not well formed, or not following its schema).</summary>
public sealed class InvoiceXmlException(string message) : Exception(message);
