namespace Edx.IntakeService.Application.Storage;

/// <summary>
/// Where the bytes of the received files are written (section <c>Storage</c>). The physical place is a setting, so the service moves to another store (or another bucket, or another root) by changing the settings and the
/// registration of <c>IBlobStore</c>, with no change of code; the folder hierarchy is in the database and does not move with it.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>The container (bucket) of the blob store the files go to.</summary>
    public string Container { get; set; } = "edx-intake";

    /// <summary>A path in front of every object name (<c>inbox</c> gives <c>inbox/{tenant}/2026/10/...</c>); empty for none.</summary>
    public string RootPath { get; set; } = string.Empty;
}
