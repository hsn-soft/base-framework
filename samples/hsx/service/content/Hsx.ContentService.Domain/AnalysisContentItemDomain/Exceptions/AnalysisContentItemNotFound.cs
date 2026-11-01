using HsnSoft.Base;

namespace Hsx.ContentService.Domain.AnalysisContentItemDomain.Exceptions;

/// <summary>There is no such analysis content item: answered as 404.</summary>
public static class AnalysisContentItemNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"AnalysisContentItem {id} was not found.").WithKey("analysis_content_item_not_found", id);
}
