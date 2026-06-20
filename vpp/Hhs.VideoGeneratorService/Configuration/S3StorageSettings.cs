using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class S3StorageSettings : StorageProviderSettingsBase
{
    public string? Region { get; set; } = "us-east-1";
}
