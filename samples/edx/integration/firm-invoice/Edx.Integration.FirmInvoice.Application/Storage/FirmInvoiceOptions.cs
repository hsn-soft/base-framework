using Microsoft.Extensions.Configuration;

namespace Edx.Integration.FirmInvoice.Application.Storage;

/// <summary>What the service accepts (section <c>FirmInvoice</c>). The limits are the service's, whatever the gateway in front of it allows.</summary>
public sealed class FirmInvoiceOptions
{
    public const string SectionName = "FirmInvoice";

    /// <summary>The name of the host that received an invoice (<c>Edx.Integration.FirmInvoice.Http</c>, <c>.Grpc</c> or <c>.Wcf</c>): each host of the service sets its own, and it is kept in the queue item and sent with the event.</summary>
    public string ServiceName { get; set; } = "Edx.Integration.FirmInvoice";

    /// <summary>The biggest file (a zip, or a model) the service takes, in bytes.</summary>
    public long MaxUploadBytes { get; set; } = 104_857_600;

    /// <summary>The most the file in a zip may weigh when unpacked, in bytes: a zip that is small and unpacks to gigabytes is refused.</summary>
    public long MaxUnpackedBytes { get; set; } = 524_288_000;

    /// <summary>The most a file may weigh (<c>FirmInvoice:MaxUploadBytes</c>): what Kestrel, the form, WCF and the application service all allow.</summary>
    public static long MaxUploadBytesOf(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetValue($"{SectionName}:{nameof(MaxUploadBytes)}", new FirmInvoiceOptions().MaxUploadBytes);
    }
}
