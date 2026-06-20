namespace Hhs.Shared.Configuration.Providers.Storage;

public sealed class LocalStorageSettings : StorageProviderSettingsBase
{
    public LocalStorageSettings()
    {
        Type = "Local";
    }

    /// <summary>
    /// Local file system path where files are stored
    /// </summary>
    public string LocalPath { get; set; } = "/tmp/cdn-storage";
}
