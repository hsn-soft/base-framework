using HsnSoft.Base.Application.Dtos;

namespace Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos;

public sealed class VideoRequestDto : AuditedEntityDto<Guid>
{
    public string ScopeKey { get; init; } = string.Empty;
    public Guid RefContentId { get; init; }
    public string RefContentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CurrentMilestone { get; init; } = string.Empty;
    public string MediaInputJson { get; init; } = string.Empty;
    public string? AudioProviderKey { get; init; }
    public string VideoProviderKey { get; init; } = string.Empty;
    public string? VideoProviderTrackingId { get; init; }
    public string? VideoProviderUrl { get; init; }
    public string? VideoStorageUrl { get; init; }
    public string? VideoCdnUrl { get; init; }
    public int RetryCount { get; init; }
    public string? LastError { get; init; }
}
