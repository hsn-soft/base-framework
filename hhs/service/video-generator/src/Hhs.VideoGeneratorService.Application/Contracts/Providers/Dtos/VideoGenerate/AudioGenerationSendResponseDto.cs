using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public sealed class AudioGenerationSendResponseDto
{
    public bool HasError { get; set; }

    [CanBeNull]
    public string ErrorMessage { get; set; }

    [CanBeNull]
    public string AudioFileName { get; set; }
}