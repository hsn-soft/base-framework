using System.Runtime.Serialization;
using CoreWCF;
using Edx.Shared.Contracts.Invoicing;

namespace Edx.Integration.FirmInvoice.Wcf.Host;

/// <summary>The names the WCF contract is known by to its clients (they come out in the WSDL).</summary>
public static class FirmInvoiceWcfNames
{
    public const string Namespace = "urn:edx:firm-invoice:v1";
    public const string Address = "/wcf/firm-invoice.svc";
}

/// <summary>
/// The WCF (SOAP) entry point, for the customers whose systems speak it: the same two things as REST and gRPC; every call carries the JWT bearer token in the HTTP header <c>Authorization</c>. A zip travels as the bytes of the message (base64 in the XML), so what a WCF
/// customer can send is bounded by what a message may weigh (<c>FirmInvoice:MaxUploadBytes</c>); a bigger zip goes over REST or gRPC, which stream it. The answer is that the invoice was queued; what became of it is read from the queue.
/// </summary>
[ServiceContract(Name = "FirmInvoiceService", Namespace = FirmInvoiceWcfNames.Namespace)]
public interface IFirmInvoiceWcfService
{
    [OperationContract(Name = "UploadInvoiceFile")]
    [FaultContract(typeof(FirmInvoiceFault))]
    Task<QueueItemResult> UploadInvoiceFileAsync(InvoiceFileUploadRequest request);

    [OperationContract(Name = "SubmitInvoice")]
    [FaultContract(typeof(FirmInvoiceFault))]
    Task<QueueItemResult> SubmitInvoiceAsync(InvoiceSubmitRequest request);
}

/// <summary>A zip with one file, and what the sender says of it.</summary>
[DataContract(Name = "InvoiceFileUploadRequest", Namespace = FirmInvoiceWcfNames.Namespace)]
public sealed class InvoiceFileUploadRequest
{
    /// <summary>The name of the zip.</summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string FileName { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public Guid InvoiceId { get; set; }

    [DataMember(Order = 3, IsRequired = true)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>The SHA-256 of the zip, in hex.</summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary><c>Xml</c>, <c>Csv</c>, <c>Txt</c> or <c>Xls</c>: the type of the file in the zip.</summary>
    [DataMember(Order = 5, IsRequired = true)]
    public InvoiceSourceType SourceType { get; set; }

    [DataMember(Order = 6, IsRequired = true)]
    public byte[] Content { get; set; } = [];
}

[DataContract(Name = "InvoiceSubmitRequest", Namespace = FirmInvoiceWcfNames.Namespace)]
public sealed class InvoiceSubmitRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public InvoiceSubmissionDto Invoice { get; set; } = new();

    /// <summary>The file the model is kept as: <c>Json</c> or <c>Xml</c>.</summary>
    [DataMember(Order = 2)]
    public string Format { get; set; } = "Json";
}

/// <summary>What the service says of the queue item an invoice became.</summary>
[DataContract(Name = "QueueItemResult", Namespace = FirmInvoiceWcfNames.Namespace)]
public sealed class QueueItemResult
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

/// <summary>What a WCF client is told when a call fails: the code of the error (<c>upload_hash_mismatch</c>, <c>unauthorized</c>, ...) and a message; an unknown failure has only <c>internal_error</c> and a trace id.</summary>
[DataContract(Name = "FirmInvoiceFault", Namespace = FirmInvoiceWcfNames.Namespace)]
public sealed class FirmInvoiceFault
{
    [DataMember(Order = 1)]
    public string Code { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}
