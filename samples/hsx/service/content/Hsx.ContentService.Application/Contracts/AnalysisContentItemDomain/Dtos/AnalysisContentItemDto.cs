using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Dtos;

public sealed class AnalysisContentItemDto : EntityDto<Guid>
{
    public string ScopeKey { get; init; } = string.Empty;
    public Guid AnalysisContentId { get; init; }
    public Guid CustomerContentId { get; init; }
    public int SortOrder { get; init; }
    public string? SourceTitle { get; init; }
    public string? SourceDescription { get; init; }
    public string? SourceImageUrl { get; init; }
}
