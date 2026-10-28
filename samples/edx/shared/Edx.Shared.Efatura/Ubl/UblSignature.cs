using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Edx.Shared.Efatura.Ubl;

/// <summary>
/// The signature of an invoice as the guides ask for it (Ek-3: XAdES-BES, enveloped, in the extension of the UBL document: <c>ds:Signature</c> with <c>SignedInfo</c>, <c>SignatureValue</c>, <c>KeyInfo</c> and the <c>xades:QualifyingProperties</c> object). <see cref="AddFake"/> puts one that has the
/// <b>shape</b> and none of the cryptography: the digest is the SHA-256 of the document, but the signature value and the certificate are made up. A real signer (a financial seal, a certificate) replaces it; the center only checks that a signature is there.
/// </summary>
public static class UblSignature
{
    private static readonly XNamespace s_ext = UblDocumentNames.Ext;
    private static readonly XNamespace s_ds = UblDocumentNames.XmlDsig;
    private static readonly XNamespace s_xades = UblDocumentNames.XAdES;

    /// <summary>The document with a (fake) signature in its extension, whatever was there (a placeholder, an earlier signature); the document given is not changed.</summary>
    public static XElement AddFake(XElement invoice, DateTime signingTime, string signerName)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        XElement signed = new(invoice);
        XElement content = signed.Element(s_ext + "UBLExtensions")?.Elements(s_ext + "UBLExtension").Select(extension => extension.Element(s_ext + "ExtensionContent")).FirstOrDefault(element => element is not null)
                           ?? throw new InvalidOperationException("The invoice has no UBL extension to put a signature in.");

        // what is signed is the document without the signature
        content.ReplaceNodes(new XElement(XNamespace.Get("urn:edx:unsigned") + "Unsigned"));
        string digest = Convert.ToBase64String(SHA256.HashData(Canonical(signed)));
        string certificate = Convert.ToBase64String(Encoding.UTF8.GetBytes($"EDX-FAKE-CERTIFICATE:{signerName}"));
        string certificateDigest = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(certificate)));

        content.ReplaceNodes(
            new XElement(
                s_ds + "Signature",
                new XAttribute(XNamespace.Xmlns + "ds", UblDocumentNames.XmlDsig),
                new XAttribute(XNamespace.Xmlns + "xades", UblDocumentNames.XAdES),
                new XAttribute("Id", "Signature"),
                new XElement(
                    s_ds + "SignedInfo",
                    new XAttribute("Id", "SignedInfo"),
                    new XElement(s_ds + "CanonicalizationMethod", new XAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315")),
                    new XElement(s_ds + "SignatureMethod", new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256")),
                    new XElement(
                        s_ds + "Reference",
                        new XAttribute("URI", string.Empty),
                        new XElement(s_ds + "Transforms", new XElement(s_ds + "Transform", new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#enveloped-signature"))),
                        new XElement(s_ds + "DigestMethod", new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#sha256")),
                        new XElement(s_ds + "DigestValue", digest)),
                    new XElement(
                        s_ds + "Reference",
                        new XAttribute("URI", "#SignedProperties"),
                        new XAttribute("Type", "http://uri.etsi.org/01903#SignedProperties"),
                        new XElement(s_ds + "DigestMethod", new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#sha256")),
                        new XElement(s_ds + "DigestValue", certificateDigest))),
                new XElement(s_ds + "SignatureValue", Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("EDX-FAKE-SIGNATURE:" + digest)))),
                new XElement(
                    s_ds + "KeyInfo",
                    new XElement(s_ds + "KeyValue", new XElement(s_ds + "RSAKeyValue", new XElement(s_ds + "Modulus", certificateDigest), new XElement(s_ds + "Exponent", "AQAB"))),
                    new XElement(s_ds + "X509Data", new XElement(s_ds + "X509SubjectName", $"CN={signerName}"), new XElement(s_ds + "X509Certificate", certificate))),
                new XElement(
                    s_ds + "Object",
                    new XElement(
                        s_xades + "QualifyingProperties",
                        new XAttribute("Target", "#Signature"),
                        new XElement(
                            s_xades + "SignedProperties",
                            new XAttribute("Id", "SignedProperties"),
                            new XElement(
                                s_xades + "SignedSignatureProperties",
                                new XElement(s_xades + "SigningTime", signingTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                                new XElement(
                                    s_xades + "SigningCertificate",
                                    new XElement(
                                        s_xades + "Cert",
                                        new XElement(s_xades + "CertDigest", new XElement(s_ds + "DigestMethod", new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#sha256")), new XElement(s_ds + "DigestValue", certificateDigest)),
                                        new XElement(s_xades + "IssuerSerial", new XElement(s_ds + "X509IssuerName", "CN=Edx Fake CA"), new XElement(s_ds + "X509SerialNumber", "1")))),
                                new XElement(s_xades + "SignerRole", new XElement(s_xades + "ClaimedRoles", new XElement(s_xades + "ClaimedRole", "Supplier")))))))));
        return signed;
    }

    // the document in a form that does not depend on how it was written (enough for a digest that is not checked by anyone)
    private static byte[] Canonical(XElement document)
    {
        using MemoryStream stream = new();
        using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false, Encoding = new UTF8Encoding(false) }))
        {
            document.Save(writer);
        }

        return stream.ToArray();
    }
}
