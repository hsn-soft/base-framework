#:property PublishAot=false
#:project ../../shared/Edx.Shared.Efatura/Edx.Shared.Efatura.csproj

// A sender unit: makes an envelope of a sample invoice of the UBL-TR package (with a new number and uuid, and a signature skeleton that is present but not real), hands it to the mock center and asks what became of it until it is done.
//   dotnet run send-invoice.cs -- [sender alias] [receiver alias] [invoice number] [nosig|badxml]
// An argument of "-" is the default: from Veriban's sender unit to Bilgera's post box. "nosig" leaves the signature out (1161), "badxml" breaks a value (1160).
using System.Xml.Linq;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Client;
using Edx.Shared.Efatura.Envelope;

string Arg(int index, string fallback) => args.Length > index && args[index] != "-" ? args[index] : fallback;

string senderAlias = Arg(0, "urn:mail:veriban_gb@veriban.example");
string receiverAlias = Arg(1, "urn:mail:bilgera_pk@bilgera.example");
string number = Arg(2, "GIB" + DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) + "01");
string mutation = Arg(3, "none");
string center = Environment.GetEnvironmentVariable("CENTER") ?? "http://localhost:5700/EFaturaMerkez/services/EFatura/";

// Veriban and Bilgera are the participants of the center with these register numbers (see the settings of the center)
EnvelopeParty sender = new(senderAlias, "1234567801", "Veriban Yazilim A.S.");
EnvelopeParty receiver = new(receiverAlias, receiverAlias.Contains("diger", StringComparison.Ordinal) ? "1234567803" : "1234567802", receiverAlias.Contains("diger", StringComparison.Ordinal) ? "Diger Entegrator Musterisi Ltd." : "Bilgera Bilisim A.S.");

string directory = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(directory, "HsnSoft.Base.sln")))
{
    directory = Path.GetDirectoryName(directory) ?? throw new InvalidOperationException("Run it inside the repository.");
}

XElement invoice = XElement.Load(Path.Combine(directory, "samples/edx/docs/einvoice/UBLTR_1.2.1_Paketi/xml/TemelFaturaOrnegi.xml"));
XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
XNamespace ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
XNamespace ds = "http://www.w3.org/2000/09/xmldsig#";
invoice.Element(cbc + "ID")!.Value = number;
invoice.Element(cbc + "UUID")!.Value = Guid.NewGuid().ToString().ToUpperInvariant();
// the invoice is for the receiver of the envelope (an envelope holds the invoices of one post box)
XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
XElement customerId = invoice.Element(cac + "AccountingCustomerParty")!.Element(cac + "Party")!.Elements(cac + "PartyIdentification").First().Element(cbc + "ID")!;
customerId.SetAttributeValue("schemeID", "VKN");
customerId.Value = receiver.RegisterNumber;
if (mutation != "nosig")
{
    // the center checks that a signature is there, never that it is right: a skeleton of one is enough
    invoice.Element(ext + "UBLExtensions")!.Element(ext + "UBLExtension")!.Element(ext + "ExtensionContent")!.ReplaceNodes(
        new XElement(
            ds + "Signature",
            new XAttribute("Id", "Signature"),
            new XElement(
                ds + "SignedInfo",
                new XElement(ds + "CanonicalizationMethod", new XAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315")),
                new XElement(ds + "SignatureMethod", new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#rsa-sha1")),
                new XElement(ds + "Reference", new XAttribute("URI", string.Empty), new XElement(ds + "DigestMethod", new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha1")), new XElement(ds + "DigestValue", "AAAA"))),
            new XElement(ds + "SignatureValue", "AAAA")));
}

if (mutation == "badxml")
{
    invoice.Element(cbc + "IssueDate")!.Value = "not-a-date";
}

(string id, byte[] zip) = EnvelopeBuilder.BuildInvoiceEnvelope(sender, receiver, invoice, DateTime.Now);
using EFaturaClient client = new(new Uri(center));
Console.WriteLine($"envelope {id}, invoice {number} ({mutation})");
EFaturaResult sent = await client.SendAsync(id, zip);
Console.WriteLine($"sendDocument: {(sent.Accepted ? "taken" : "refused")} {sent.Code} {sent.Message}");
int last = 0;
for (int attempt = 0; attempt < 30 && sent.Accepted; attempt++)
{
    await Task.Delay(2000);
    EFaturaResult answer = await client.QueryAsync(id);
    if (!answer.Accepted)
    {
        Console.WriteLine($"getApplicationResponse: {answer.Code} {answer.Message}");
        continue;
    }

    SystemResponse response = SystemResponse.FromElement(EnvelopeReader.ReadXml(System.Text.Encoding.UTF8.GetBytes(answer.ApplicationResponse!)).Elements[0]);
    if (response.ResultCode != last)
    {
        Console.WriteLine($"status: {response.ResultCode} {response.ResultDescription}");
    }

    last = response.ResultCode;
    if (response.ResultCode is 1300 or 1215 or 1230 or (>= 1110 and < 1200))
    {
        break;
    }
}
