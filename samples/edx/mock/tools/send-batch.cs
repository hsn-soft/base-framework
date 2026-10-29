#:property PublishAot=false
#:project ../../shared/Edx.Shared.Efatura/Edx.Shared.Efatura.csproj

// A sender unit that hands the center ONE envelope with many invoices (the post: one envelope, one post box, up to 1000 invoices), and asks for its status with the uuid of the envelope until it is done.
//   dotnet run send-batch.cs -- [count] [ok|mixed]
// count is the number of invoices (default 5; 1001 is refused: an envelope holds at most 1000). "mixed" puts an invoice for another post box in the envelope (refused: 1181).
using System.Xml.Linq;
using Edx.Shared.Efatura.Client;
using Edx.Shared.Efatura.Envelope;

int count = args.Length > 0 && args[0] != "-" ? int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture) : 5;
string mode = args.Length > 1 ? args[1] : "ok";
string center = Environment.GetEnvironmentVariable("CENTER") ?? "http://localhost:5700/EFaturaMerkez/services/EFatura/";

EnvelopeParty sender = new("urn:mail:veriban_gb@veriban.example", "1234567801", "Veriban Yazilim A.S.");
EnvelopeParty receiver = new("urn:mail:bilgera_pk@bilgera.example", "1234567802", "Bilgera Bilisim A.S.");

string directory = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(directory, "HsnSoft.Base.sln")))
{
    directory = Path.GetDirectoryName(directory) ?? throw new InvalidOperationException("Run it inside the repository.");
}

XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
XNamespace ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
XNamespace ds = "http://www.w3.org/2000/09/xmldsig#";
string path = Path.Combine(directory, "samples/edx/docs/einvoice/UBLTR_1.2.1_Paketi/xml/TemelFaturaOrnegi.xml");
string stamp = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);

List<XElement> invoices = [];
for (int index = 1; index <= count; index++)
{
    XElement invoice = XElement.Load(path);
    invoice.Element(cbc + "ID")!.Value = $"B{stamp}{index:D6}";
    invoice.Element(cbc + "UUID")!.Value = Guid.NewGuid().ToString().ToUpperInvariant();
    XElement customerId = invoice.Element(cac + "AccountingCustomerParty")!.Element(cac + "Party")!.Elements(cac + "PartyIdentification").First().Element(cbc + "ID")!;
    customerId.SetAttributeValue("schemeID", "VKN");
    customerId.Value = mode == "mixed" && index == count ? "1234567801" : receiver.RegisterNumber;
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
    invoices.Add(invoice);
}

string id = Guid.NewGuid().ToString();
EnvelopeHeader header = EnvelopeHeader.Create(sender, receiver, EnvelopeTypes.Sender, id, DateTime.Now, ElementTypes.Invoice, invoices.Count);
byte[] zip = EnvelopeBuilder.Zip(id, EnvelopeBuilder.BuildXml(header, invoices));
using EFaturaClient client = new(new Uri(center));
Console.WriteLine($"envelope {id}: {invoices.Count} invoices ({mode}), {zip.Length} bytes");
EFaturaResult sent = await client.SendAsync(id, zip);
Console.WriteLine($"sendDocument: {(sent.Accepted ? "taken" : "refused")} {sent.Code} {sent.Message}");
int last = 0;
for (int attempt = 0; attempt < 60 && sent.Accepted; attempt++)
{
    await Task.Delay(1000);
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
