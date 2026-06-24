using Hhs.Shared.Helper.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;

public sealed class AudioRequest : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Correlation & Context
    public string? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid VideoRequestId { get; set; }
    public Guid RefContentId { get; set; }
    public ContentType RefContentType { get; set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Status & Configuration
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;
    public int SortOrder { get; set; }

    // Input Data
    public string InputText { get; set; } = default!;

    // Provider Configuration
    public string AudioProviderKey { get; set; } = default!;

    // Audio Generation (external provider)
    public string? AudioProviderTrackingId { get; set; }
    public string? AudioProviderUrl { get; set; }
    public string? AudioLocalPath { get; set; }
    public string? AudioStorageUrl { get; set; }
    public string? AudioCdnUrl { get; set; }
    public string? AudioCdnProviderKey { get; set; }

    // Polling & Retry
    public DateTime? NextProviderPollAtUtc { get; set; }
    public int ProviderPollingCount { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    private AudioRequest() { }

    public AudioRequest(Guid id, Guid videoRequestId, Guid refContentId, ContentType refContentType, string scopeKey, Guid sourceEventId, string inputText, string audioProviderKey, int sortOrder)
    {
        Id = id;
        VideoRequestId = videoRequestId;
        RefContentId = refContentId;
        RefContentType = refContentType;
        ScopeKey = scopeKey;
        SourceEventId = sourceEventId;
        InputText = inputText;
        AudioProviderKey = audioProviderKey;
        SortOrder = sortOrder;
    }
}
