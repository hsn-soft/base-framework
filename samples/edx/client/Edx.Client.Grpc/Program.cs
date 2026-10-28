using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Edx.Client.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

// Sends one invoice file to Edx over gRPC: zips it (the file is not zipped yet), gives the SHA-256 of the zip and streams it in chunks.
//   dotnet run -- <file> <invoice-number> [invoice-uuid]
// Settings (environment): EDX_GATEWAY (the REST address, for the token; default http://localhost:5600), EDX_GRPC (default http://localhost:5601), EDX_TENANT, EDX_USER, EDX_PASSWORD.
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
using GrpcChannel channel = GrpcChannel.ForAddress(Environment.GetEnvironmentVariable("EDX_GRPC") ?? "http://localhost:5601");
Intake.IntakeClient client = new(channel);
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
