namespace Hhs.ContentService.Entities;

public sealed class AnalysisContentItem
{
    // Identity & Relationships
    public Guid Id { get; set; }
    public Guid AnalysisContentId { get; set; }
    public AnalysisContent AnalysisContent { get; set; } = default!;
    public Guid CustomerContentId { get; set; }
    public CustomerContent CustomerContent { get; set; } = default!;

    // Ordering
    public int SortOrder { get; set; }
}
