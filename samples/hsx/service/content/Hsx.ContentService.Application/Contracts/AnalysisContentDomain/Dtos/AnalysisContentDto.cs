using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Dtos;

public sealed class AnalysisContentDto : AuditedEntityDto<Guid>
{
    public string ScopeKey { get; init; } = string.Empty;
    public DateTimeOffset AnalysisDate { get; init; }
    public string? NormalizeStatus { get; init; }
    public string? VideoStatus { get; init; }
    public string? VideoCdnUrl { get; init; }
    public string? CurrentMilestone { get; init; }
    public string? LastError { get; init; }
}
