using HsnSoft.Base.Domain.Sorting;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Entities;

namespace Hsx.ContentService.Domain.AnalysisContentItemDomain.Repositories;

/// <summary>The fields a list of analysis content items may be sorted by and the sorting when it sends none.</summary>
public static class AnalysisContentItemSorting
{
    public const string Default = "sortOrder asc";

    public static readonly SortFields<AnalysisContentItem> Fields = new SortFields<AnalysisContentItem>()
        .Add("sortOrder", entity => entity.SortOrder);
}
