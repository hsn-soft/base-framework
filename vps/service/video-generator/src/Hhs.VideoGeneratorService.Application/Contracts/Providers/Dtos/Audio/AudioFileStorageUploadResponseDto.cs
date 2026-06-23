using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Audio;

public sealed class AudioFileStorageUploadResponseDto
{
    public bool HasError { get; set; }

    [CanBeNull]
    public string ErrorMessage { get; set; }

    [CanBeNull]
    public string AudioTraceId { get; set; }

    [CanBeNull]
    public string AudioUrl { get; set; }
}