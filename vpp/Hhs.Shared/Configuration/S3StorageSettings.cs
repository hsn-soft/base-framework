namespace Hhs.Shared.Configuration;

public sealed class S3StorageSettings : StorageProviderSettingsBase
{
    public string? Region { get; set; } = "us-east-1";
}
