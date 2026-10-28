using Edx.Shared.Efatura.Server;
using Edx.Shared.Efatura.Wire;

namespace Edx.Mock.Center.Wcf.Host.Services;

/// <summary>
/// The WCF service of the center (the contract of the real one: <c>sendDocument</c>, <c>getApplicationResponse</c>). It asks the request's own scope for what it needs, hands over to <see cref="EnvelopeIntake"/> and says a refusal as the fault of the center
/// (<see cref="EFaturaFaultType"/>: a code and a message).
/// </summary>
public sealed class CenterService(IHttpContextAccessor accessor) : IEFaturaPortType
{
    public async Task<SendDocumentReply> SendDocumentAsync(SendDocumentCall request)
    {
        EnvelopeIntake intake = Intake();
        DocumentType? document = request?.Document;
        try
        {
            string message = await intake.ReceiveAsync(document?.FileName, document?.BinaryData?.Value, document?.Hash, CancellationToken()).ConfigureAwait(false);
            return new SendDocumentReply { Response = new DocumentReturnType { Msg = message, Hash = document!.Hash } };
        }
        catch (EFaturaFaultException fault)
        {
            throw fault.ToFault();
        }
    }

    public async Task<GetApplicationResponseReply> GetApplicationResponseAsync(GetApplicationResponseCall request)
    {
        EnvelopeIntake intake = Intake();
        try
        {
            string xml = await intake.QueryAsync(request?.Request?.InstanceIdentifier, CancellationToken()).ConfigureAwait(false);
            return new GetApplicationResponseReply { Response = new GetAppRespResponseType { ApplicationResponse = xml } };
        }
        catch (EFaturaFaultException fault)
        {
            throw fault.ToFault();
        }
    }

    private EnvelopeIntake Intake()
        => (accessor.HttpContext ?? throw new InvalidOperationException("The call has no HTTP context.")).RequestServices.GetRequiredService<EnvelopeIntake>();

    private CancellationToken CancellationToken() => accessor.HttpContext?.RequestAborted ?? default;
}
