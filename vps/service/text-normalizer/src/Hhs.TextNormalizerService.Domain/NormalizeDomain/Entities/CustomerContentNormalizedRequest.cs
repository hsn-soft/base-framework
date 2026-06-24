using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using HsnSoft.Base.Domain.Entities.Auditing;
using System.Diagnostics.CodeAnalysis;
using HsnSoft.Base;
using HsnSoft.Base.Subscribe;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;

public sealed class CustomerContentNormalizedRequest : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Correlation
    public string? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Content Reference
    public Guid CustomerContentId { get; set; }
    public string DomainName { get; set; } = default!;
    public string ContentKey { get; set; } = default!;

    // Status & Progress
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    // Scraping State
    public string? ScrapingStatus { get; set; }
    public ScrapingResult? ScrapingResult { get; set; }

    // Outline Generation State
    public string? OutlineStatus { get; set; }
    public OutlineResult? OutlineResult { get; set; }

    // Outline Polling & Tracking
    public string? OutlineProviderTrackId { get; set; }
    public DateTime? NextOutlinePollAtUtc { get; set; }
    public int OutlinePollingCount { get; set; }
    public int MaxOutlinePollingCount { get; set; } = 60;

    // Retry Configuration
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    private CustomerContentNormalizedRequest() { }

    public CustomerContentNormalizedRequest(Guid id, string scopeKey, Guid customerContentId, string domainName, string contentKey, Guid? correlationId = null)
    {
        Id = id;
        ScopeKey = scopeKey;
        CustomerContentId = customerContentId;
        DomainName = domainName;
        ContentKey = contentKey;
        CorrelationId = correlationId?.ToString();
    }
}
