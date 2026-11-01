using HsnSoft.Base.Domain.Sorting;
using Hsx.ContentService.Domain.AnalysisContentDomain.Entities;

namespace Hsx.ContentService.Domain.AnalysisContentDomain.Repositories;

/// <summary>The fields a list of analysis contents may be sorted by and the sorting when it sends none.</summary>
public static class AnalysisContentSorting
{
    public const string Default = "analysisDate desc";

    public static readonly SortFields<AnalysisContent> Fields = new SortFields<AnalysisContent>()
        .Add("analysisDate", entity => entity.AnalysisDate)
        .Add("createdAt", entity => entity.CreatedAt);
}
