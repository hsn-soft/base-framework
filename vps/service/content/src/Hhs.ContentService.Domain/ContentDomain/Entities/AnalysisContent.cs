using System.Diagnostics.CodeAnalysis;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Domain.Entities.SoftDeleting;
using HsnSoft.Base.Domain.Entities.Subscriptions;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AnalysisContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Content Metadata
    public string DomainName { get; private set; } = default!;
    public string? Title { get; private set; }

    // Correlation & Tracing
    public string? CorrelationId { get; private set; }

    // Normalization Status
    public string? NormalizeStatus { get; set; }
    public Guid? NormalizeRequestId { get; set; }

    // Video Generation Status
    public string? VideoStatus { get; set; }
    public Guid? VideoRequestId { get; set; }

    // Result & Errors
    public string? FinalVideoUrl { get; set; }
    public string? LastFacility { get; set; }
    public string? LastError { get; set; }

    // Analysis Items
    public List<AnalysisContentItem> Items { get; private set; } = [];

    private AnalysisContent() { }

    public AnalysisContent(Guid id, string scopeKey, string domainName, string? title = null, string? correlationId = null)
    {
        Id = id;
        ScopeKey = scopeKey;
        DomainName = domainName;
        Title = title;
        CorrelationId = correlationId;
    }
}
