using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.DashboardDomain.Dtos;

public sealed class GetContentTextDetailResultDto
{
    public int TotalCount { get; set; }
    public List<ContentItemDto> Items { get; set; }
}

public sealed class ContentItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    [CanBeNull] public string Spot { get; set; }
    public string OperationStatus { get; set; }
    public string ContentUrl { get; set; }
    public DateTime ReleaseTime { get; set; }
    [CanBeNull] public string Summary { get; set; }
    [CanBeNull] public string ImageUrl { get; set; }
}
