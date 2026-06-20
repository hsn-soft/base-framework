using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Storage;

public sealed class S3StorageSettings : StorageProviderSettingsBase
{
    public const string SectionName = "Provider:Storage:S3";

    public string? Region { get; set; } = "us-east-1";
}
