using System.Runtime.Serialization;
using System.ServiceModel;

namespace Edx.Client.Wcf;

// The contract of the Edx firm invoice integration, as its WSDL (…/firm-invoice-service/wcf/firm-invoice.svc?wsdl) says; a customer may generate it with dotnet-svcutil instead of writing it.
internal static class FirmInvoiceNames
{
    public const string Namespace = "urn:edx:firm-invoice:v1";
}

[ServiceContract(Name = "FirmInvoiceService", Namespace = FirmInvoiceNames.Namespace)]
internal interface IFirmInvoiceService
{
    [OperationContract(Name = "UploadInvoiceFile")]
    [FaultContract(typeof(FirmInvoiceFault))]
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

[DataContract(Name = "InvoiceFileUploadRequest", Namespace = FirmInvoiceNames.Namespace)]
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

[DataContract(Name = "QueueItemResult", Namespace = FirmInvoiceNames.Namespace)]
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

[DataContract(Name = "FirmInvoiceFault", Namespace = FirmInvoiceNames.Namespace)]
internal sealed class FirmInvoiceFault
{
    [DataMember(Order = 1)]
    public string Code { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}
