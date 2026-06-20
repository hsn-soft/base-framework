namespace Hhs.Shared.Configuration.Providers.Storage;

public sealed class AzureBlobStorageSettings : StorageProviderSettingsBase
{
    public string? AccountName { get; set; }
}
