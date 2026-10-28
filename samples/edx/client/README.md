# Edx customer examples

Three small console programs a customer can start from. Each one takes **one invoice file that is not zipped yet** (`.xml`, `.txt`, `.csv`, `.xls` or `.xlsx`), makes the zip **in memory** (one file in it), computes the **SHA-256 of the zip** and sends it to the Edx
firm invoice integration with the uuid and the number of the invoice, in the way the customer's system can speak:

| Program | Protocol | Reached directly (the token comes from the gateway, `http://localhost:5600`) |
|---|---|---|
| [Edx.Client.Rest](Edx.Client.Rest) | REST (multipart form) | `POST /api/firm-invoice-service/v1/commercial/archives/upload` (`http://localhost:5510`) |
| [Edx.Client.Grpc](Edx.Client.Grpc) | gRPC (client stream) | `edx.firm_invoice.v1.FirmInvoice/UploadInvoiceFile` (`http://localhost:5511`, HTTP/2) |
| [Edx.Client.Wcf](Edx.Client.Wcf) | WCF / SOAP | `POST /wcf/firm-invoice.svc` (`http://localhost:5512`), WSDL at `?wsdl` |

Whatever the way, the rules are the same (the firm invoice integration checks them before it answers): the file is a **zip with exactly one file**, the type of that file (`sourceType`) matches its extension, the hash is the **SHA-256 (hex) of the zip**, the invoice has a
**uuid** (`invoiceId`) and a **number** (`invoiceNumber`), and the zip is not bigger than the service accepts. The answer says the invoice was **queued**; what became of it (draft created, rejected, failed, completed) is read from the queue
(`POST /api/firm-invoice-service/v1/commercial/queue/get/{queueId}` or `.../queue/by-invoice/{invoiceId}` on `http://localhost:5510`) or seen on the queue page of the back office.

An invoice that is already a draft, approved or signed is not taken again (send another invoice number); one that failed or was rejected may be sent again with the same uuid, which replaces it.

## Running them

The programs need a user of the customer's tenant (the sample users are in `service/identity/Edx.IdentityService.Http.Host/appsettings.development.json`; `samples/edx/run-all.sh` starts the system):

```bash
export EDX_TENANT=veriban EDX_USER=operator EDX_PASSWORD='<the password of that user>'
# optional: EDX_GATEWAY (the token, default http://localhost:5600), EDX_FIRM_INVOICE_REST (:5510), EDX_FIRM_INVOICE_GRPC (:5511), EDX_FIRM_INVOICE_WCF (:5512)

cd samples/edx/client/Edx.Client.Rest      # or Edx.Client.Grpc, or Edx.Client.Wcf
dotnet run -- ./invoice.csv ABC2026000000001                       # a new uuid is made
dotnet run -- ./invoice.csv ABC2026000000001 7b0c6a3e-...-4f1e     # or the one the customer's system has
```

Samples of the files are in [../docs](../docs). A token is taken first (`POST /auth-server/v1/auth/login` with the tenant code, the user name and the password) and goes in the header `Authorization: Bearer <token>` of every call (in the
`authorization` metadata for gRPC). The user's role must have the permission of the endpoint (`endpoint.firm-invoice.upload`) and the constraint `constraint.firm-invoice.upload.max-bytes` that says how big a zip it may send.

## What a customer needs of Edx

* REST: nothing but HTTP.
* gRPC: the proto file ([Edx.Client.Grpc/Protos/firm_invoice.proto](Edx.Client.Grpc/Protos/firm_invoice.proto)) and `Grpc.Net.Client`, `Google.Protobuf`, `Grpc.Tools`.
* WCF: the WSDL (or the contract below) and `System.ServiceModel.Http`. A SOAP message carries the zip as base64, so a WCF customer sends what a message may weigh (100 MB at most); a bigger zip goes over REST or gRPC, which stream it.

## The code

### REST: `Edx.Client.Rest/Program.cs`

```csharp
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

// Sends one invoice file to Edx over REST: zips it (the file is not zipped yet), gives the SHA-256 of the zip and uploads it.
//   dotnet run -- <file> <invoice-number> [invoice-uuid]
// Settings (environment): EDX_GATEWAY (where the token comes from, default http://localhost:5600), EDX_FIRM_INVOICE_REST (the firm invoice integration is reached directly, default http://localhost:5510), EDX_TENANT, EDX_USER, EDX_PASSWORD.
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Edx.Client.Rest <file> <invoice-number> [invoice-uuid]");
    return 2;
}

string path = args[0];
string invoiceNumber = args[1];
Guid invoiceId = args.Length > 2 ? Guid.Parse(args[2]) : Guid.NewGuid();
string gateway = Environment.GetEnvironmentVariable("EDX_GATEWAY") ?? "http://localhost:5600";
string firmInvoice = Environment.GetEnvironmentVariable("EDX_FIRM_INVOICE_REST") ?? "http://localhost:5510";

using HttpClient http = new() { BaseAddress = new Uri(gateway), Timeout = TimeSpan.FromMinutes(5) };

// 1. who am I: a token of the tenant's user
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
string sourceType = SourceTypeOf(path);
byte[] zip = Zip(path);
string hash = Convert.ToHexStringLower(SHA256.HashData(zip));

// 3. the upload: a form with the zip and what is said of it
using MultipartFormDataContent form = new();
using ByteArrayContent file = new(zip);
file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
form.Add(file, "file", Path.GetFileNameWithoutExtension(path) + ".zip");
form.Add(new StringContent(invoiceId.ToString()), "invoiceId");
form.Add(new StringContent(invoiceNumber), "invoiceNumber");
form.Add(new StringContent(hash), "fileHash");
form.Add(new StringContent(sourceType), "sourceType");

using HttpRequestMessage request = new(HttpMethod.Post, new Uri(new Uri(firmInvoice), "api/firm-invoice-service/v1/commercial/archives/upload")) { Content = form };
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
using HttpResponseMessage response = await http.SendAsync(request);
Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}");
Console.WriteLine(await response.Content.ReadAsStringAsync());
return response.IsSuccessStatusCode ? 0 : 1;

static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set.");

// the type of the file in the zip: Xml, Txt, Csv or Xls (an .xlsx is Xls too)
static string SourceTypeOf(string file) => Path.GetExtension(file).ToLowerInvariant() switch
{
    ".xml" => "Xml",
    ".txt" => "Txt",
    ".csv" => "Csv",
    ".xls" or ".xlsx" => "Xls",
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
```

### gRPC: `Edx.Client.Grpc/Program.cs`

```csharp
using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Edx.Client.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

// Sends one invoice file to Edx over gRPC: zips it (the file is not zipped yet), gives the SHA-256 of the zip and streams it in chunks.
//   dotnet run -- <file> <invoice-number> [invoice-uuid]
// Settings (environment): EDX_GATEWAY (the REST address, for the token; default http://localhost:5600), EDX_FIRM_INVOICE_GRPC (the firm invoice integration is reached directly; default http://localhost:5511), EDX_TENANT, EDX_USER, EDX_PASSWORD.
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Edx.Client.Grpc <file> <invoice-number> [invoice-uuid]");
    return 2;
}

string path = args[0];
string invoiceNumber = args[1];
Guid invoiceId = args.Length > 2 ? Guid.Parse(args[2]) : Guid.NewGuid();

// 1. who am I: a token of the tenant's user (the token comes from the REST address)
using HttpClient http = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("EDX_GATEWAY") ?? "http://localhost:5600") };
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
SourceType sourceType = SourceTypeOf(path);
byte[] zip = Zip(path);
string hash = Convert.ToHexStringLower(SHA256.HashData(zip));

// 3. the upload: the first message is the header, the next ones carry the bytes of the zip
using GrpcChannel channel = GrpcChannel.ForAddress(Environment.GetEnvironmentVariable("EDX_FIRM_INVOICE_GRPC") ?? "http://localhost:5511");
FirmInvoiceService.FirmInvoiceServiceClient client = new(channel);
Metadata headers = new() { { "authorization", $"Bearer {token}" } };
try
{
    using AsyncClientStreamingCall<UploadChunk, QueueReply> call = client.UploadInvoiceFile(headers);
    await call.RequestStream.WriteAsync(new UploadChunk
    {
        Header = new UploadHeader
        {
            FileName = Path.GetFileNameWithoutExtension(path) + ".zip",
            InvoiceId = invoiceId.ToString(),
            InvoiceNumber = invoiceNumber,
            FileHash = hash,
            SourceType = sourceType,
        },
    });
    const int ChunkSize = 64 * 1024;
    for (int offset = 0; offset < zip.Length; offset += ChunkSize)
    {
        await call.RequestStream.WriteAsync(new UploadChunk { Data = Google.Protobuf.ByteString.CopyFrom(zip, offset, Math.Min(ChunkSize, zip.Length - offset)) });
    }

    await call.RequestStream.CompleteAsync();
    QueueReply reply = await call.ResponseAsync;
    Console.WriteLine($"queued: {reply.QueueId} ({reply.InvoiceNumber}, {reply.SourceType}, {reply.Status})");
    return 0;
}
catch (RpcException error)
{
    Console.Error.WriteLine($"{error.StatusCode}: {error.Status.Detail}");
    return 1;
}

static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is not set.");

// the type of the file in the zip (an .xlsx is Xls too)
static SourceType SourceTypeOf(string file) => Path.GetExtension(file).ToLowerInvariant() switch
{
    ".xml" => SourceType.Xml,
    ".txt" => SourceType.Txt,
    ".csv" => SourceType.Csv,
    ".xls" or ".xlsx" => SourceType.Xls,
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
```

### WCF: `Edx.Client.Wcf/Program.cs` and `FirmInvoiceContract.cs`

```csharp
using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text.Json;
using Edx.Client.Wcf;

// Sends one invoice file to Edx over WCF (SOAP): zips it (the file is not zipped yet), gives the SHA-256 of the zip and sends it as the bytes of the message.
//   dotnet run -- <file> <invoice-number> [invoice-uuid]
// Settings (environment): EDX_GATEWAY (where the token comes from, default http://localhost:5600), EDX_FIRM_INVOICE_WCF (the firm invoice integration is reached directly, default http://localhost:5512), EDX_TENANT, EDX_USER, EDX_PASSWORD. A zip may weigh what a SOAP message may (100 MB at most); a bigger one goes over REST or gRPC.
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Edx.Client.Wcf <file> <invoice-number> [invoice-uuid]");
    return 2;
}

string path = args[0];
string invoiceNumber = args[1];
Guid invoiceId = args.Length > 2 ? Guid.Parse(args[2]) : Guid.NewGuid();
string gateway = Environment.GetEnvironmentVariable("EDX_GATEWAY") ?? "http://localhost:5600";
string firmInvoice = Environment.GetEnvironmentVariable("EDX_FIRM_INVOICE_WCF") ?? "http://localhost:5512";

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
ChannelFactory<IFirmInvoiceService> factory = new(binding, new EndpointAddress(new Uri(new Uri(firmInvoice), "wcf/firm-invoice.svc")));
IFirmInvoiceService service = factory.CreateChannel();
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
catch (FaultException<FirmInvoiceFault> fault)
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
```

```csharp
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Edx.Client.Wcf;

// The contract of the Edx firm invoice integration, as its WSDL (…/wcf/firm-invoice.svc?wsdl) says; a customer may generate it with dotnet-svcutil instead of writing it.
internal static class FirmInvoiceNames
{
    public const string Namespace = "urn:edx:firm-invoice:v1";
}

[ServiceContract(Name = "FirmInvoiceService", Namespace = FirmInvoiceNames.Namespace)]
internal interface IFirmInvoiceService
{
    [OperationContract(Name = "UploadInvoiceFile")]
    [FaultContract(typeof(FirmInvoiceFault))]
    Task<QueueItemResult> UploadInvoiceFileAsync(InvoiceFileUploadRequest request);
}

// the numbers are the ones of the service
internal enum InvoiceSourceType
{
    Xml = 2,
    Txt = 3,
    Csv = 4,
    Xls = 5,
}

[DataContract(Name = "InvoiceFileUploadRequest", Namespace = FirmInvoiceNames.Namespace)]
internal sealed class InvoiceFileUploadRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public string FileName { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public Guid InvoiceId { get; set; }

    [DataMember(Order = 3, IsRequired = true)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [DataMember(Order = 4, IsRequired = true)]
    public string FileHash { get; set; } = string.Empty;

    [DataMember(Order = 5, IsRequired = true)]
    public InvoiceSourceType SourceType { get; set; }

    [DataMember(Order = 6, IsRequired = true)]
    public byte[] Content { get; set; } = [];
}

[DataContract(Name = "QueueItemResult", Namespace = FirmInvoiceNames.Namespace)]
internal sealed class QueueItemResult
{
    [DataMember(Order = 1)]
    public Guid QueueId { get; set; }

    [DataMember(Order = 2)]
    public Guid InvoiceId { get; set; }

    [DataMember(Order = 3)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string SourceType { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string ChannelType { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public string Status { get; set; } = string.Empty;

    [DataMember(Order = 7)]
    public string FileName { get; set; } = string.Empty;

    [DataMember(Order = 8)]
    public string FileHash { get; set; } = string.Empty;

    [DataMember(Order = 9)]
    public long SizeBytes { get; set; }

    [DataMember(Order = 10)]
    public Guid TenantId { get; set; }
}

[DataContract(Name = "FirmInvoiceFault", Namespace = FirmInvoiceNames.Namespace)]
internal sealed class FirmInvoiceFault
{
    [DataMember(Order = 1)]
    public string Code { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}
```
