#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false
#:project ../../shared/Edx.Shared.Efatura/Edx.Shared.Efatura.csproj

// A post box of an integrator, as small as it can be, to try the mock center with until the post box of Edx is there: it takes what the center delivers, writes it down, answers the center with a system response (as a
// post box does, with sendDocument) and keeps the answer for getApplicationResponse. Set SILENT=1 to make it not answer by itself, so the center has to ask. Listens on http://localhost:5541/EFatura/.
//   dotnet run stub-postbox.cs
using CoreWCF;
using CoreWCF.Configuration;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Client;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Server;
using Edx.Shared.Efatura.Wire;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => { options.ListenLocalhost(5541); options.AllowSynchronousIO = true; });
builder.Services.AddServiceModelServices();
builder.Services.AddSingleton<PostBox>();
var app = builder.Build();
app.UseServiceModel(services => services.AddService<PostBox>().AddServiceEndpoint<PostBox, IEFaturaPortType>(EFaturaServerBinding.Create(false, 10 * 1024 * 1024), "/EFatura/"));
app.Run();

public class PostBox : IEFaturaPortType
{
    private static readonly Dictionary<string, string> s_answers = [];
    private static readonly bool s_silent = Environment.GetEnvironmentVariable("SILENT") == "1";
    private static readonly Uri s_center = new(Environment.GetEnvironmentVariable("CENTER") ?? "http://localhost:5700/EFaturaMerkez/services/EFatura/");

    public Task<GetApplicationResponseReply> GetApplicationResponseAsync(GetApplicationResponseCall request)
    {
        string id = request.Request!.InstanceIdentifier!;
        Console.WriteLine($"asked about {id}: {(s_answers.ContainsKey(id) ? "answered" : "no answer")}");
        return s_answers.TryGetValue(id, out string? xml)
            ? Task.FromResult(new GetApplicationResponseReply { Response = new GetAppRespResponseType { ApplicationResponse = xml } })
            : throw new FaultException<EFaturaFaultType>(new EFaturaFaultType { Code = EfaturaStatus.EnvelopeNotFound, Msg = EfaturaStatus.NameOf(EfaturaStatus.EnvelopeNotFound) }, "no such envelope");
    }

    public Task<SendDocumentReply> SendDocumentAsync(SendDocumentCall request)
    {
        DocumentType document = request.Document!;
        byte[] zip = document.BinaryData!.Value!;
        Console.WriteLine($"got {document.FileName} ({zip.Length} bytes, md5 {(Hashing.Md5Hex(zip) == document.Hash ? "right" : "WRONG")})");
        EnvelopeContent content = EnvelopeReader.ReadZip(zip);
        EnvelopeHeader header = content.Header;
        Console.WriteLine($"  {header.DocumentType} from {header.Sender.Alias} to {header.Receiver.Alias}: {content.Elements.Count} x {content.Elements.FirstOrDefault()?.Name.LocalName}");
        if (header.DocumentType == EnvelopeTypes.System)
        {
            SystemResponse about = SystemResponse.FromElement(content.Elements[0]);
            Console.WriteLine($"  it says of {about.ReferenceEnvelopeId}: {about.ResultCode} {about.ResultDescription}");
        }
        else
        {
            // the answer: from the address the envelope was sent to, to the address it came from
            SystemResponse response = new(header.InstanceIdentifier, header.DocumentType, header.CreatedAt, EfaturaStatus.Processed, EfaturaStatus.NameOf(EfaturaStatus.Processed), DateTime.Now, header.Receiver, header.Sender);
            (string id, byte[] xml, byte[] answerZip) = response.ToEnvelope();
            s_answers[header.InstanceIdentifier] = System.Text.Encoding.UTF8.GetString(xml);
            if (!s_silent)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    using EFaturaClient client = new(s_center);
                    EFaturaResult result = await client.SendAsync(id, answerZip);
                    Console.WriteLine($"  answered the center: {(result.Accepted ? "taken" : $"refused {result.Code} {result.Message}")}");
                });
            }
        }

        return Task.FromResult(new SendDocumentReply { Response = new DocumentReturnType { Msg = EfaturaStatus.NameOf(EfaturaStatus.Queued), Hash = document.Hash } });
    }
}
