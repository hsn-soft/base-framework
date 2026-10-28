using Microsoft.Extensions.Configuration;

namespace Edx.IntakeService.Application.Storage;

/// <summary>What the service accepts (section <c>Intake</c>). The limits are the service's, whatever the gateway in front of it allows.</summary>
public sealed class IntakeOptions
{
    public const string SectionName = "Intake";

    /// <summary>The name of the host that received an invoice (<c>Edx.IntakeService.Http</c>, <c>.Grpc</c> or <c>.Wcf</c>): each host of the service sets its own, and it is kept in the queue item and sent with the event.</summary>
    public string ServiceName { get; set; } = "Edx.IntakeService";

    /// <summary>The biggest file (a zip, or a model) the service takes, in bytes.</summary>
    public long MaxUploadBytes { get; set; } = 104_857_600;

    /// <summary>The most the file in a zip may weigh when unpacked, in bytes: a zip that is small and unpacks to gigabytes is refused.</summary>
    public long MaxUnpackedBytes { get; set; } = 524_288_000;

    /// <summary>The most a file may weigh (<c>Intake:MaxUploadBytes</c>): what Kestrel, the form, WCF and the application service all allow.</summary>
    public static long MaxUploadBytesOf(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetValue($"{SectionName}:{nameof(MaxUploadBytes)}", new IntakeOptions().MaxUploadBytes);
    }
}
