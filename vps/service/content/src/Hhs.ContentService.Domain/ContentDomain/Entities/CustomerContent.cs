using Hhs.ContentService.Domain.ContentDomain.Consts;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;
using Hhs.Shared.Helper;
using Hhs.Shared.Localization;
using HsnSoft.Base.Text;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class CustomerContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; }

    // Content Metadata
    [NotNull] public string ContentKey { get; private set; }
    [NotNull] public string SlugKey { get; private set; }

    // Correlation
    [CanBeNull] public string CorrelationId { get; private set; }

    // Normalization Status
    [CanBeNull] public string NormalizeStatus { get; set; }
    [CanBeNull] public Guid? NormalizeRequestId { get; set; }
    public DateTime? ScrapReleaseTimeUtc { get; private set; }

    // Video Generation Status
    [CanBeNull] public string VideoStatus { get; set; }
    [CanBeNull] public Guid? AudioRequestId { get; set; }
    [CanBeNull] public Guid? VideoRequestId { get; set; }
    [CanBeNull] public string VideoCdnUrl { get; set; }

    [CanBeNull] public string LastFacility { get; set; }
    [CanBeNull] public string LastError { get; set; }

    private CustomerContent()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        ContentKey = string.Empty;
        SlugKey = string.Empty;
    }

    internal CustomerContent([NotNull] string scopeKey, [NotNull] string contentKey, [CanBeNull] string correlationId = null)
        : this(id: Guid.CreateVersion7(), scopeKey: scopeKey,  contentKey: contentKey, correlationId: correlationId)
    {
    }

    internal CustomerContent(Guid id, [NotNull] string scopeKey, [NotNull] string contentKey, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetScopeKey(scopeKey);
        SetContentKey(contentKey);

        CorrelationId = correlationId;
    }

    private void SetScopeKey(string scopeKey)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            scopeKey,
            $"{nameof(CustomerContent)}:{nameof(ScopeKey)}",
            CustomerContentConsts.ScopeKeyMaxLength
        );

    private void SetContentKey(string contentKey)
    {
        string checkContentKey = LocalizedModelValidator.NotNullOrWhiteSpace(contentKey, $"{nameof(CustomerContent)}:{nameof(ContentKey)}", CustomerContentConsts.ContentKeyMaxLength);
        ContentKey = StringHelper.Minimize(checkContentKey);
        SlugKey = StringHelper.SlugKeyNormalize(ContentKey);
    }

    public void SetScrapReleaseTimeUtc(DateTime? scrapReleaseTimeUtc)
    {
        if (scrapReleaseTimeUtc.HasValue && scrapReleaseTimeUtc.Value == default)
        {
            throw new ArgumentException($"{nameof(ScrapReleaseTimeUtc)} is invalid", nameof(scrapReleaseTimeUtc));
        }

        ScrapReleaseTimeUtc = scrapReleaseTimeUtc?.ToUniversalTime();
    }
}