using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public sealed class VideoGenerationQueryRequestDto
{
    [NotNull]
    public string ExternalVideoTraceId { get; set; }
}