using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;
using Hhs.Shared.Helper;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class CustomerContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; } = default!;

    // Content Metadata
    [NotNull] public string DomainName { get; private set; } = default!;
    [NotNull] public string ContentKey { get; private set; } = default!;
    [NotNull] public string SlugKey { get; private set; } = default!;

    // Correlation & Tracing
    [CanBeNull]
    public string CorrelationId { get; private set; }

    // Normalization Status
    [CanBeNull]  public string NormalizeStatus { get; set; }
    [CanBeNull] public Guid? NormalizeRequestId { get; set; }

    // Video Generation Status
    [CanBeNull]  public string VideoStatus { get; set; }
    [CanBeNull] public Guid? VideoRequestId { get; set; }
    [CanBeNull] public Guid? AudioRequestId { get; set; }

    // Result & Errors
    [CanBeNull]  public string FinalVideoUrl { get; set; }
    [CanBeNull]  public string LastFacility { get; set; }
    [CanBeNull]  public string LastError { get; set; }

    private CustomerContent() { }

    public CustomerContent(Guid id, string scopeKey, string domainName, string contentKey, string slugKey, [CanBeNull] string correlationId = null)
    {
        Id = id;
        ScopeKey = scopeKey;
        DomainName = domainName;
        ContentKey = contentKey;
        SlugKey = slugKey;
        CorrelationId = correlationId;
        NormalizeStatus = StatusNames.NotStarted;
        VideoStatus = StatusNames.NotStarted;
    }
}