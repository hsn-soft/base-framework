using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class CustomerContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Content Metadata
    public string DomainName { get; private set; } = default!;
    public string ContentKey { get; private set; } = default!;
    public string SlugKey { get; private set; } = default!;

    // Correlation & Tracing
    public string? CorrelationId { get; private set; }

    // Normalization Status
    public string? NormalizeStatus { get; set; }
    public Guid? NormalizeRequestId { get; set; }

    // Video Generation Status
    public string? VideoStatus { get; set; }
    public Guid? VideoRequestId { get; set; }
    public Guid? AudioRequestId { get; set; }

    // Result & Errors
    public string? FinalVideoUrl { get; set; }
    public string? LastFacility { get; set; }
    public string? LastError { get; set; }

    private CustomerContent() { }

    public CustomerContent(Guid id, string scopeKey, string domainName, string contentKey, string slugKey, string? correlationId = null)
    {
        Id = id;
        ScopeKey = scopeKey;
        DomainName = domainName;
        ContentKey = contentKey;
        SlugKey = slugKey;
        CorrelationId = correlationId;
    }
}