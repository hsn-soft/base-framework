using HsnSoft.Base.Application.Dtos;

namespace Hsx.ContentService.Application.Contracts.CustomerContentDomain.Dtos;

public sealed class CustomerContentDto : AuditedEntityDto<Guid>
{
    public string ScopeKey { get; init; } = string.Empty;
    public string ContentKey { get; init; } = string.Empty;
    public string SlugKey { get; init; } = string.Empty;
    public string? NormalizeStatus { get; init; }
    public string? VideoStatus { get; init; }
    public string? VideoCdnUrl { get; init; }
    public string? CurrentMilestone { get; init; }
    public string? LastError { get; init; }
}
