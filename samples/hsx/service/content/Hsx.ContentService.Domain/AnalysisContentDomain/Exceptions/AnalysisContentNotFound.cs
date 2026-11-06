using HsnSoft.Base;

namespace Hsx.ContentService.Domain.AnalysisContentDomain.Exceptions;

/// <summary>There is no such analysis content: answered as 404.</summary>
public static class AnalysisContentNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"AnalysisContent {id} was not found.").WithKey("analysis_content_not_found", id);
}
