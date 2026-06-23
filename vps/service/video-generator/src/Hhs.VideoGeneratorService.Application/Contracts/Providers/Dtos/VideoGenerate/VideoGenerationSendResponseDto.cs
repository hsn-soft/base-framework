using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public sealed class VideoGenerationSendResponseDto
{
    public bool HasError { get; set; }

    [CanBeNull]
    public string ErrorMessage { get; set; }

    [CanBeNull]
    public string ExternalVideoTraceId { get; set; }
}