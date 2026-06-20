namespace Hhs.VideoGeneratorService.Configuration;

public sealed class CdnProviderConfiguration
{
    public const string SectionName = "CdnProvider";
    public string Default { get; set; } = "cdn-local-minio";
}
