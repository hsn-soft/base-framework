using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using HsnSoft.Base.Domain.Entities.Auditing;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;

public sealed class CustomerContentNormalizedRequest : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Correlation
    [CanBeNull]
    public string? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Content Reference
    public Guid CustomerContentId { get; set; }
    [NotNull] public string DomainName { get; set; } = default!;
    [NotNull] public string ContentKey { get; set; } = default!;

    // Status & Progress
    [NotNull] public string Status { get; set; } = default!;
    [NotNull] public string CurrentStep { get; set; } = default!;

    // Scraping State
    [CanBeNull] public string? ScrapingStatus { get; set; }
    [CanBeNull] public ScrapingResult? ScrapingResult { get; set; }

    // Outline Generation State
    [CanBeNull] public string? OutlineStatus { get; set; }
    [CanBeNull] public OutlineResult? OutlineResult { get; set; }

    // Outline Polling & Tracking
    [CanBeNull] public string? OutlineProviderTrackId { get; set; }
    [CanBeNull] public DateTime? NextOutlinePollAtUtc { get; set; }
    public int OutlinePollingCount { get; set; }
    public int MaxOutlinePollingCount { get; set; } = CustomerContentNormalizedRequestConsts.MaxOutlinePollingCountDefault;

    // Retry Configuration
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = CustomerContentNormalizedRequestConsts.MaxRetryCountDefault;
    [CanBeNull] public DateTime? NextRetryAtUtc { get; set; }
    [CanBeNull] public string? LastError { get; set; }

    private CustomerContentNormalizedRequest() { }

    public CustomerContentNormalizedRequest(Guid id, string scopeKey, Guid customerContentId, string domainName, string contentKey, string? correlationId = null)
    {
        Id = id;
        ScopeKey = scopeKey;
        CustomerContentId = customerContentId;
        DomainName = domainName;
        ContentKey = contentKey;
        CorrelationId = correlationId;
    }
}
