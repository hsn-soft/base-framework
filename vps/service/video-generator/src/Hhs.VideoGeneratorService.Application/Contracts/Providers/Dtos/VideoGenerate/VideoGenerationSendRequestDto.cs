using Hhs.Shared.Helper.Enums;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;

public sealed class VideoGenerationSendRequestDto
{
    public Guid VideoRequestId { get; set; }

    public ReferenceContentTypes RefContentType { get;  set; }

    [NotNull]
    public List<VideoContentDataDto> VideoContentDatas { get;  set; }

    [CanBeNull]
    public List<string> AudioFileNames { get; set; }
}

public sealed class VideoContentDataDto
{
    public string TitleText { get; set; }

    [NotNull]
    public string NormalizedContent { get; set; }

    public string ImageUrl { get; set; }
}