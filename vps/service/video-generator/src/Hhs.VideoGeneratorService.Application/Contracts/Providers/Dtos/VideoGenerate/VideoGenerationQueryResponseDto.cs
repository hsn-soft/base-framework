using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public sealed class VideoGenerationQueryResponseDto
{
    public bool HasError { get; set; }

    [CanBeNull]
    public string ErrorMessage { get; set; }

    public bool IsVideoReady { get; set; }

    [CanBeNull]
    public string ExternalVideoUrl { get; set; }

    [CanBeNull]
    public string StorageVideoUrl { get; set; }
}