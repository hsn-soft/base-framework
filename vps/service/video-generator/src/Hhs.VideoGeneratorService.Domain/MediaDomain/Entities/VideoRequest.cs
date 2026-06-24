using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Auditing;
using System.Diagnostics.CodeAnalysis;
using HsnSoft.Base;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;

public sealed class VideoRequest : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Correlation & Context
    [CanBeNull]
    public string? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid RefContentId { get; set; }
    public ContentType RefContentType { get; set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Status & Configuration
    [NotNull] public string Status { get; set; } = default!;
    [NotNull] public string CurrentStep { get; set; } = default!;

    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Input Data
    [NotNull] public string MediaInputJson { get; set; } = default!;

    // Provider Configuration
    [CanBeNull] public string? AudioProviderKey { get; set; }
    [NotNull] public string VideoProviderKey { get; set; } = default!;

    // Video Generation (external provider)
    [CanBeNull] public string? VideoProviderTrackingId { get; set; }
    [CanBeNull] public string? VideoProviderUrl { get; set; }
    [CanBeNull] public string? VideoLocalPath { get; set; }

    // Storage & CDN
    [CanBeNull] public string? VideoCdnProviderKey { get; set; }
    [CanBeNull] public string? VideoStorageUrl { get; set; }
    [CanBeNull] public string? VideoCdnUrl { get; set; }

    // Polling & Retry
    [CanBeNull] public DateTime? NextProviderPollAtUtc { get; set; }
    public int ProviderPollingCount { get; set; }
    public int RetryCount { get; set; }
    [CanBeNull] public DateTime? NextRetryAtUtc { get; set; }
    [CanBeNull] public string? LastError { get; set; }

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
