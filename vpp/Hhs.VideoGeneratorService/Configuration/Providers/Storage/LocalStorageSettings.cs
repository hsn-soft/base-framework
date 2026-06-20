using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Storage;

public sealed class LocalStorageSettings : StorageProviderSettingsBase
{
    public const string SectionName = "Provider:Storage:Local";

    public LocalStorageSettings()
    {
        Type = "Local";
    }

    /// <summary>
    /// Local file system path where files are stored
    /// </summary>
    public string LocalPath { get; set; } = "/tmp/cdn-storage";
}
