using Hhs.Shared.Helper.Enums;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline;

public class OutlineRequestDto
{
    public string OutlinePrompt { get; set; }

    public ReferenceContentTypes RefContentType { get; set; }

    public Guid RefContentId { get; set; }

    public string OutlineInput { get; set; }
}