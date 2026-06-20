using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class AzureBlobStorageSettings : StorageProviderSettingsBase
{
    public string? AccountName { get; set; }
}
