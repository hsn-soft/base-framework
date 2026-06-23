using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GetAnalysisVideoDetailResultDto
{
    public int TotalCount { get; set; }
    public List<AnalysisVideoItemDto> Items { get; set; }
}

public sealed class AnalysisVideoItemDto
{
    public Guid Id { get; set; }
    public Guid RefContentId { get; set; }
    public string OperationStatus { get; set; }
    public DateTime CreationTime { get; set; }
    [CanBeNull] public string StorageVideoUrl { get; set; }
    public List<NormalizedContentDataDto> NormalizedContentDatas { get; set; }
}

public sealed class NormalizedContentDataDto
{
    [CanBeNull] public string TitleText { get; set; }
    [NotNull] public string NormalizedContent { get; set; }
    [CanBeNull] public string ImageUrl { get; set; }
}

