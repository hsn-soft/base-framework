using Edx.Integration.GibInvoice.Application.Services;
using Edx.Shared.Efatura.Server;
using Edx.Shared.Efatura.Wire;
using HsnSoft.Base.MultiTenancy;

namespace Edx.Integration.GibInvoice.Wcf.Host;

/// <summary>
/// The post box of the tenants as a WCF service (the contract of the real center: <c>sendDocument</c>, <c>getApplicationResponse</c>). It asks the request's own scope for what it needs and acts as the system tenant (an envelope that comes in belongs to no tenant until it is read: the owner of the alias
/// it is for is found by the REST host), hands over to <see cref="InboundReceiver"/> and says a refusal as the fault of the exchange (<see cref="EFaturaFaultType"/>: a code and a message).
/// </summary>
public sealed class PostBoxService(IHttpContextAccessor accessor) : IEFaturaPortType
{
    public async Task<SendDocumentReply> SendDocumentAsync(SendDocumentCall request)
    {
        DocumentType? document = request?.Document;
        try
        {
            using IDisposable system = SystemTenant();
            string message = await Receiver().ReceiveAsync(document?.FileName, document?.BinaryData?.Value, document?.Hash, CancellationToken()).ConfigureAwait(false);
            return new SendDocumentReply { Response = new DocumentReturnType { Msg = message, Hash = document!.Hash } };
        }
        catch (EFaturaFaultException fault)
        {
            throw fault.ToFault();
        }
    }

    public async Task<GetApplicationResponseReply> GetApplicationResponseAsync(GetApplicationResponseCall request)
    {
        try
        {
            using IDisposable system = SystemTenant();
            string xml = await Receiver().QueryAsync(request?.Request?.InstanceIdentifier, CancellationToken()).ConfigureAwait(false);
            return new GetApplicationResponseReply { Response = new GetAppRespResponseType { ApplicationResponse = xml } };
        }
        catch (EFaturaFaultException fault)
        {
            throw fault.ToFault();
        }
    }

    private IDisposable SystemTenant() => Services().GetRequiredService<ICurrentTenant>().Change(null, isSystemTenant: true);

    private InboundReceiver Receiver() => Services().GetRequiredService<InboundReceiver>();

    private IServiceProvider Services() => (accessor.HttpContext ?? throw new InvalidOperationException("The call has no HTTP context.")).RequestServices;

    private CancellationToken CancellationToken() => accessor.HttpContext?.RequestAborted ?? default;
}
