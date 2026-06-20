using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Storage;

public sealed class AzureBlobStorageSettings : StorageProviderSettingsBase
{
    public const string SectionName = "Provider:Storage:Azure";

    public string? AccountName { get; set; }
}
