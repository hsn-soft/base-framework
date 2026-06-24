using HsnSoft.Base.Domain.Entities;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AnalysisContentItem : Entity<Guid>
{
    // Identity & Relationships
    public Guid AnalysisContentId { get; set; }
    public AnalysisContent AnalysisContent { get; set; } = default!;
    public Guid CustomerContentId { get; set; }
    public CustomerContent CustomerContent { get; set; } = default!;

    // Ordering
    public int SortOrder { get; set; }


    private AnalysisContentItem()
    {
    }

    public AnalysisContentItem(Guid id, Guid analysisContentId, Guid customerContentId, int sortOrder)
    {
        Id = id;
        AnalysisContentId = analysisContentId;
        CustomerContentId = customerContentId;
        SortOrder = sortOrder;
    }
}