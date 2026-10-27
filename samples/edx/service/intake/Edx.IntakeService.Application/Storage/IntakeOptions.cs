namespace Edx.IntakeService.Application.Storage;

/// <summary>What the service accepts (section <c>Intake</c>). The limits are the service's, whatever the gateway in front of it allows.</summary>
public sealed class IntakeOptions
{
    public const string SectionName = "Intake";

    /// <summary>The biggest file (an archive, or a model) the service takes, in bytes.</summary>
    public long MaxUploadBytes { get; set; } = 104_857_600;

    /// <summary>The most files an archive may hold.</summary>
    public int MaxArchiveEntries { get; set; } = 1_000;

    /// <summary>The most all the files of an archive may weigh when unpacked, in bytes: an archive that is small and unpacks to gigabytes is refused.</summary>
    public long MaxUnpackedBytes { get; set; } = 524_288_000;

    /// <summary>The extensions an archive of tables may hold (with the dot, in any case).</summary>
    public string[] TabularExtensions { get; set; } = [".txt", ".csv", ".xls", ".xlsx"];
}
