using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Domain.Entities;
using System.Diagnostics.CodeAnalysis;
using HsnSoft.Base;
using HsnSoft.Base.Subscribe;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;

public sealed class VideoRequest : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Correlation & Context
    public string? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid RefContentId { get; set; }
    public ContentType RefContentType { get; set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Status & Configuration
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Input Data
    public string MediaInputJson { get; set; } = default!;

    // Provider Configuration
    public string? AudioProviderKey { get; set; }
    public string VideoProviderKey { get; set; } = default!;

    // Video Generation (external provider)
    public string? VideoProviderTrackingId { get; set; }
    public string? VideoProviderUrl { get; set; }
    public string? VideoLocalPath { get; set; }

    // Storage & CDN
    public string? VideoCdnProviderKey { get; set; }
    public string? VideoStorageUrl { get; set; }
    public string? VideoCdnUrl { get; set; }

    // Polling & Retry
    public DateTime? NextProviderPollAtUtc { get; set; }
    public int ProviderPollingCount { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    private VideoRequest() { }

    public VideoRequest(Guid id, string scopeKey, Guid refContentId, ContentType refContentType, Guid sourceEventId)
    {
        Id = id;
        ScopeKey = scopeKey;
        RefContentId = refContentId;
        RefContentType = refContentType;
        SourceEventId = sourceEventId;
    }
}
