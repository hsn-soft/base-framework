using System.Runtime.Serialization;
using CoreWCF;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;

namespace Edx.IntakeService.Http.Host.WcfApi;

/// <summary>The names the WCF contract is known by to its clients (they come out in the WSDL).</summary>
public static class IntakeWcfNames
{
    public const string Namespace = "urn:edx:intake:v1";
    public const string Address = "/wcf/intake.svc";
}

/// <summary>
/// The WCF (SOAP) entry point, for the customers whose systems speak it. The same three things as REST and gRPC; every call carries the JWT bearer token in the HTTP header <c>Authorization</c>. An archive travels as the bytes of the
/// message (base64 in the XML), so what a WCF customer can send is bounded by what a message may weigh (<c>Intake:MaxUploadBytes</c>); a bigger archive goes over REST or gRPC, which stream it.
/// </summary>
[ServiceContract(Name = "IntakeService", Namespace = IntakeWcfNames.Namespace)]
public interface IIntakeWcfService
{
    [OperationContract(Name = "UploadXmlArchive")]
    [FaultContract(typeof(IntakeFault))]
    Task<StoredFileResult> UploadXmlArchiveAsync(ArchiveUploadRequest request);

    [OperationContract(Name = "UploadTabularArchive")]
    [FaultContract(typeof(IntakeFault))]
    Task<StoredFileResult> UploadTabularArchiveAsync(ArchiveUploadRequest request);

    [OperationContract(Name = "SubmitInvoice")]
    [FaultContract(typeof(IntakeFault))]
    Task<StoredFileResult> SubmitInvoiceAsync(InvoiceSubmitRequest request);
}

[DataContract(Name = "ArchiveUploadRequest", Namespace = IntakeWcfNames.Namespace)]
public sealed class ArchiveUploadRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public string FileName { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public byte[] Content { get; set; } = [];
}

[DataContract(Name = "InvoiceSubmitRequest", Namespace = IntakeWcfNames.Namespace)]
public sealed class InvoiceSubmitRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public InvoiceSubmissionDto Invoice { get; set; } = new();

    /// <summary>The file the model is kept as: <c>Json</c> or <c>Xml</c>.</summary>
    [DataMember(Order = 2)]
    public string Format { get; set; } = nameof(ModelFormat.Json);
}

[DataContract(Name = "StoredFileResult", Namespace = IntakeWcfNames.Namespace)]
public sealed class StoredFileResult
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public string Kind { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Channel { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string FileName { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string ContentType { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public long SizeBytes { get; set; }

    [DataMember(Order = 7)]
    public string FolderPath { get; set; } = string.Empty;

    [DataMember(Order = 8)]
    public string Container { get; set; } = string.Empty;

    [DataMember(Order = 9)]
    public string ObjectName { get; set; } = string.Empty;

    [DataMember(Order = 10)]
    public int EntryCount { get; set; }

    [DataMember(Order = 11)]
    public Guid TenantId { get; set; }
}

/// <summary>What a WCF client is told when a call fails: the code of the error (<c>archive_empty</c>, <c>file_not_found</c>, <c>unauthorized</c>, ...) and a message; an unknown failure has only <c>internal_error</c> and a trace id.</summary>
[DataContract(Name = "IntakeFault", Namespace = IntakeWcfNames.Namespace)]
public sealed class IntakeFault
{
    [DataMember(Order = 1)]
    public string Code { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}
