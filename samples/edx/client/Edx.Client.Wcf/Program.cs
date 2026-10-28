using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text.Json;
using Edx.Client.Wcf;

// Sends one invoice file to Edx over WCF (SOAP): zips it (the file is not zipped yet), gives the SHA-256 of the zip and sends it as the bytes of the message.
//   dotnet run -- <file> <invoice-number> [invoice-uuid]
// Settings (environment): EDX_GATEWAY (default http://localhost:5600), EDX_TENANT, EDX_USER, EDX_PASSWORD. A zip may weigh what a SOAP message may (100 MB at most); a bigger one goes over REST or gRPC.
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Edx.Client.Wcf <file> <invoice-number> [invoice-uuid]");
    return 2;
}

string path = args[0];
string invoiceNumber = args[1];
Guid invoiceId = args.Length > 2 ? Guid.Parse(args[2]) : Guid.NewGuid();
string gateway = Environment.GetEnvironmentVariable("EDX_GATEWAY") ?? "http://localhost:5600";

// 1. who am I: a token of the tenant's user (it is a REST call: WCF only carries the token afterwards)
using HttpClient http = new() { BaseAddress = new Uri(gateway) };
using HttpResponseMessage login = await http.PostAsJsonAsync("auth-server/v1/auth/login", new
{
    tenant = Required("EDX_TENANT"),
    username = Required("EDX_USER"),
    password = Required("EDX_PASSWORD"),
});
login.EnsureSuccessStatusCode();
using JsonDocument loginBody = await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync());
string token = loginBody.RootElement.GetProperty("payload").GetProperty("accessToken").GetString()!;

// 2. one file in a zip, made in memory, and the SHA-256 of the zip
InvoiceFileUploadRequest request = new()
{
    FileName = Path.GetFileNameWithoutExtension(path) + ".zip",
    InvoiceId = invoiceId,
    InvoiceNumber = invoiceNumber,
    SourceType = SourceTypeOf(path),
    Content = Zip(path),
};
request.FileHash = Convert.ToHexStringLower(SHA256.HashData(request.Content));

// 3. the call: the token goes in the HTTP header Authorization
BasicHttpBinding binding = new() { MaxReceivedMessageSize = 200 * 1024 * 1024, SendTimeout = TimeSpan.FromMinutes(5) };
ChannelFactory<IIntakeService> factory = new(binding, new EndpointAddress(new Uri(new Uri(gateway), "intake-service/wcf/intake.svc")));
IIntakeService service = factory.CreateChannel();
try
{
    using OperationContextScope scope = new((IContextChannel)service);
    HttpRequestMessageProperty header = new();
    header.Headers["Authorization"] = $"Bearer {token}";
    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = header;

    QueueItemResult result = await service.UploadInvoiceFileAsync(request);
    Console.WriteLine($"queued: {result.QueueId} ({result.InvoiceNumber}, {result.SourceType}, {result.Status})");
    return 0;
}
catch (FaultException<IntakeFault> fault)
{
    Console.Error.WriteLine($"{fault.Detail.Code}: {fault.Detail.Message}");
    return 1;
}
catch (FaultException fault)
{
    Console.Error.WriteLine($"fault: {fault.Message}");
    return 1;
}
finally
{
    factory.Abort();
}

static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set.");

// the type of the file in the zip (an .xlsx is Xls too)
static InvoiceSourceType SourceTypeOf(string file) => Path.GetExtension(file).ToLowerInvariant() switch
{
    ".xml" => InvoiceSourceType.Xml,
    ".txt" => InvoiceSourceType.Txt,
    ".csv" => InvoiceSourceType.Csv,
    ".xls" or ".xlsx" => InvoiceSourceType.Xls,
    var other => throw new InvalidOperationException($"A file of type '{other}' is not accepted: send .xml, .txt, .csv, .xls or .xlsx."),
};

// a zip with the one file in it
static byte[] Zip(string file)
{
    using MemoryStream stream = new();
    using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
        archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
    }

    return stream.ToArray();
}
