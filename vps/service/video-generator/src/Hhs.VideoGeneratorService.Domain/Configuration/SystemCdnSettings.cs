using Hhs.Shared.Helper.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration;

public sealed class SystemCdnSettings
{
    public string Selected { get; set; } = ProviderKeys.CdnLocalMinio;
    public string LocalDownloadPath { get; set; } = "media/downloads";
}
