using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public sealed class AudioGenerationSendRequestDto
{
    public string VideoRequestReferenceId { get; set; }

    [NotNull]
    public string AudioContent { get; set; }

    public string AudioFileName { get; set; }
}