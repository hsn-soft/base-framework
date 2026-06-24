namespace Hhs.VideoGeneratorService.Domain.Configuration;

public sealed class SystemCdnSettings
{
    public string Selected { get; set; } = "cdn-local-minio";
    public string LocalDownloadPath { get; set; } = "media/downloads";
}
