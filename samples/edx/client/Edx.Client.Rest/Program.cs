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
