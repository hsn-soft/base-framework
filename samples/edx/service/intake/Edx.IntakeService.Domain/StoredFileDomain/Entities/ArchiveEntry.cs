namespace Edx.IntakeService.Domain.StoredFileDomain.Entities;

/// <summary>One file inside an archive that was received: its name and its size when unpacked.</summary>
public sealed class ArchiveEntry
{
    // for the database driver
    private ArchiveEntry()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public static ArchiveEntry Of(string name, long sizeBytes) => new() { Name = name, SizeBytes = sizeBytes };
}
