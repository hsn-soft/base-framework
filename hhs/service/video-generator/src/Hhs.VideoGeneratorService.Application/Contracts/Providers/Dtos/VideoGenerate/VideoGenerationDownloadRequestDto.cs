using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public sealed class VideoGenerationDownloadRequestDto
{
    [NotNull]
    public string ExternalVideoUrl { get; set; }
}