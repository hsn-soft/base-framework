using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AnalysisContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Content Metadata
    [NotNull] public string DomainName { get; private set; } = default!;
    [CanBeNull]  public string Title { get; private set; }

    // Correlation & Tracing
    [CanBeNull]
    public string CorrelationId { get; private set; }

    // Normalization Status
    [CanBeNull]  public string NormalizeStatus { get; set; }
    [CanBeNull] public Guid? NormalizeRequestId { get; set; }

    // Video Generation Status
    [CanBeNull]  public string VideoStatus { get; set; }
    [CanBeNull] public Guid? VideoRequestId { get; set; }

    // Result & Errors
    [CanBeNull]  public string FinalVideoUrl { get; set; }
    [CanBeNull]  public string LastFacility { get; set; }
    [CanBeNull]  public string LastError { get; set; }

    // Analysis Items
    public List<AnalysisContentItem> Items { get; private set; } = [];

    private AnalysisContent() { }

    public AnalysisContent(Guid id, string scopeKey, string domainName, [CanBeNull] string title = null, [CanBeNull] string correlationId = null)
    {
        Id = id;
        ScopeKey = scopeKey;
        DomainName = domainName;
        Title = title;
        CorrelationId = correlationId;
        // NormalizeStatus and VideoStatus start as null, handlers will set them
    }
}
