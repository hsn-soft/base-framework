using HsnSoft.Base.Application.Dtos;

namespace Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Dtos;

public sealed class CustomerContentNormalizedRequestDto : AuditedEntityDto<Guid>
{
    public string ScopeKey { get; init; } = string.Empty;
    public Guid CustomerContentId { get; init; }
    public string DomainName { get; init; } = string.Empty;
    public string ContentKey { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CurrentMilestone { get; init; } = string.Empty;
    public string? ScrapingStatus { get; init; }
    public string? OutlineStatus { get; init; }
    public int RetryCount { get; init; }
    public string? LastError { get; init; }
}
