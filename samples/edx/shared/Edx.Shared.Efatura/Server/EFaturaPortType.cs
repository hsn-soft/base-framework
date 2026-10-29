using CoreWCF;
using Edx.Shared.Efatura.Wire;

namespace Edx.Shared.Efatura.Server;

/// <summary>The contract of the center and of a post box, as CoreWCF serves it: the same operations, names, actions, MTOM bodies and fault as the WSDL of the center (<c>EFatura.wsdl</c>).</summary>
[ServiceContract(Name = WireNames.Contract, Namespace = WireNames.Namespace)]
public interface IEFaturaPortType
{
    /// <summary>The answer of the center about an envelope (a system response document), by the uuid of the envelope.</summary>
    [OperationContract(Name = "getApplicationResponse", Action = "getApplicationResponse", ReplyAction = "getApplicationResponseResponse")]
    [FaultContract(typeof(EFaturaFaultType), Action = "getApplicationResponse", Name = "EFaturaFault")]
    [XmlSerializerFormat(SupportFaults = true)]
    Task<GetApplicationResponseReply> GetApplicationResponseAsync(GetApplicationResponseCall request);

    /// <summary>Hands the center (or a post box) an envelope: a zip with one file, named by the uuid of the envelope, and its MD5.</summary>
    [OperationContract(Name = "sendDocument", Action = "sendDocument", ReplyAction = "sendDocumentResponse")]
    [FaultContract(typeof(EFaturaFaultType), Action = "sendDocument", Name = "EFaturaFault")]
    [XmlSerializerFormat(SupportFaults = true)]
    Task<SendDocumentReply> SendDocumentAsync(SendDocumentCall request);
}

[MessageContract(IsWrapped = false)]
public class GetApplicationResponseCall
{
    [MessageBodyMember(Name = "getAppRespRequest", Namespace = WireNames.Namespace, Order = 0)]
    public GetAppRespRequestType? Request { get; set; }
}

[MessageContract(IsWrapped = false)]
public class GetApplicationResponseReply
{
    [MessageBodyMember(Name = "getAppRespResponse", Namespace = WireNames.Namespace, Order = 0)]
    public GetAppRespResponseType? Response { get; set; }
}

[MessageContract(IsWrapped = false)]
public class SendDocumentCall
{
    [MessageBodyMember(Name = "documentRequest", Namespace = WireNames.Namespace, Order = 0)]
    public DocumentType? Document { get; set; }
}

[MessageContract(IsWrapped = false)]
public class SendDocumentReply
{
    [MessageBodyMember(Name = "documentResponse", Namespace = WireNames.Namespace, Order = 0)]
    public DocumentReturnType? Response { get; set; }
}
