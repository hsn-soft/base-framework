using System.ServiceModel;
using Edx.Shared.Efatura.Wire;

namespace Edx.Shared.Efatura.Client;

/// <summary>The contract of the center as a client calls it (System.ServiceModel): the same names, actions and bodies as <c>IEFaturaPortType</c>, which is the form CoreWCF serves.</summary>
[ServiceContract(Name = WireNames.Contract, Namespace = WireNames.Namespace)]
internal interface IEFaturaClientPort
{
    [OperationContract(Name = "getApplicationResponse", Action = "getApplicationResponse", ReplyAction = "getApplicationResponseResponse")]
    [FaultContract(typeof(EFaturaFaultType), Action = "getApplicationResponse", Name = "EFaturaFault")]
    [XmlSerializerFormat(SupportFaults = true)]
    Task<ClientGetApplicationResponseReply> GetApplicationResponseAsync(ClientGetApplicationResponseCall request);

    [OperationContract(Name = "sendDocument", Action = "sendDocument", ReplyAction = "sendDocumentResponse")]
    [FaultContract(typeof(EFaturaFaultType), Action = "sendDocument", Name = "EFaturaFault")]
    [XmlSerializerFormat(SupportFaults = true)]
    Task<ClientSendDocumentReply> SendDocumentAsync(ClientSendDocumentCall request);
}

[MessageContract(IsWrapped = false)]
internal sealed class ClientGetApplicationResponseCall
{
    [MessageBodyMember(Name = "getAppRespRequest", Namespace = WireNames.Namespace, Order = 0)]
    public GetAppRespRequestType? Request { get; set; }
}

[MessageContract(IsWrapped = false)]
internal sealed class ClientGetApplicationResponseReply
{
    [MessageBodyMember(Name = "getAppRespResponse", Namespace = WireNames.Namespace, Order = 0)]
    public GetAppRespResponseType? Response { get; set; }
}

[MessageContract(IsWrapped = false)]
internal sealed class ClientSendDocumentCall
{
    [MessageBodyMember(Name = "documentRequest", Namespace = WireNames.Namespace, Order = 0)]
    public DocumentType? Document { get; set; }
}

[MessageContract(IsWrapped = false)]
internal sealed class ClientSendDocumentReply
{
    [MessageBodyMember(Name = "documentResponse", Namespace = WireNames.Namespace, Order = 0)]
    public DocumentReturnType? Response { get; set; }
}
