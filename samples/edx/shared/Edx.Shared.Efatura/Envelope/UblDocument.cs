using System.Xml.Linq;

namespace Edx.Shared.Efatura.Envelope;

/// <summary>What the center reads of an invoice or an application response without knowing the whole of it: its kind, its numbers and whether it carries a signature (present, not verified).</summary>
public static class UblDocument
{
    public const string InvoiceNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    public const string ApplicationResponseNamespace = "urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2";
    public const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    private static readonly XNamespace s_cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace s_ds = XmlDsigNamespace;

    /// <summary>The <c>ElementType</c> a document is carried as (<c>INVOICE</c>, <c>APPLICATIONRESPONSE</c>), or <see langword="null"/> for a document of another kind.</summary>
    public static string? ElementTypeOf(XElement document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.Name.NamespaceName switch
        {
            InvoiceNamespace when document.Name.LocalName == "Invoice" => ElementTypes.Invoice,
            ApplicationResponseNamespace when document.Name.LocalName == "ApplicationResponse" => ElementTypes.ApplicationResponse,
            _ => null,
        };
    }

    /// <summary>The number of the document (<c>cbc:ID</c>).</summary>
    public static string? Id(XElement document) => document.Element(s_cbc + "ID")?.Value.Trim();

    /// <summary>The universal unique number of the document (ETTN, <c>cbc:UUID</c>).</summary>
    public static string? Uuid(XElement document) => document.Element(s_cbc + "UUID")?.Value.Trim();

    /// <summary>Whether the document carries an XML signature (<c>ds:Signature</c>, in the UBL extensions): its presence, not whether it is right.</summary>
    public static bool HasSignature(XElement document) => document.Descendants(s_ds + "Signature").Any();
}
