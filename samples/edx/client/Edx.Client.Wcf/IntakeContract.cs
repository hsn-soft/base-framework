using System.Runtime.Serialization;
using System.ServiceModel;

namespace Edx.Client.Wcf;

// The contract of the Edx intake service, as its WSDL (…/intake-service/wcf/intake.svc?wsdl) says; a customer may generate it with dotnet-svcutil instead of writing it.
internal static class IntakeNames
{
    public const string Namespace = "urn:edx:intake:v1";
}

[ServiceContract(Name = "IntakeService", Namespace = IntakeNames.Namespace)]
internal interface IIntakeService
{
    [OperationContract(Name = "UploadInvoiceFile")]
    [FaultContract(typeof(IntakeFault))]
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

[DataContract(Name = "InvoiceFileUploadRequest", Namespace = IntakeNames.Namespace)]
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

[DataContract(Name = "QueueItemResult", Namespace = IntakeNames.Namespace)]
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

[DataContract(Name = "IntakeFault", Namespace = IntakeNames.Namespace)]
internal sealed class IntakeFault
{
    [DataMember(Order = 1)]
    public string Code { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}
