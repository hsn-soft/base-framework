using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Consts;

namespace Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Entities;

/// <summary>
/// The request to normalize the text of a content of a customer: scrape it, make its outline. It is seen by its scope key.
/// </summary>
public sealed class CustomerContentNormalizedRequest : FullAuditedEntity<Guid>, IScopeSubscription
{
    // for the database driver
    private CustomerContentNormalizedRequest()
    {
    }

    public string ScopeKey { get; private set; } = string.Empty;

    public Guid CustomerContentId { get; private set; }

    public string DomainName { get; private set; } = string.Empty;

    public string ContentKey { get; private set; } = string.Empty;
    public string NormalizedContentKey { get; private set; } = string.Empty;

    public string? CorrelationId { get; private set; }

    public string Status { get; private set; } = string.Empty;

    public string CurrentMilestone { get; private set; } = string.Empty;

    public string? ScrapingStatus { get; private set; }

    public string? OutlineStatus { get; private set; }

    public int RetryCount { get; private set; }

    public string? LastError { get; private set; }

    public static CustomerContentNormalizedRequest Create(string scopeKey, Guid customerContentId, string domainName, string contentKey, string? correlationId, string status, string currentMilestone, string? scrapingStatus, string? outlineStatus, int retryCount, string? lastError)
    {
        CustomerContentNormalizedRequest entity = new()
        {
            Id = Guid.CreateVersion7(),
            ScopeKey = Guard.NotNullOrWhiteSpace(scopeKey, nameof(ScopeKey), CustomerContentNormalizedRequestConsts.ScopeKeyMaxLength).Trim(),
            CustomerContentId = Guard.NotDefault<Guid>(customerContentId, nameof(CustomerContentId)),
            DomainName = Guard.NotNullOrWhiteSpace(domainName, nameof(DomainName), CustomerContentNormalizedRequestConsts.DomainNameMaxLength).Trim(),
            ContentKey = Guard.NotNullOrWhiteSpace(contentKey, nameof(ContentKey), CustomerContentNormalizedRequestConsts.ContentKeyMaxLength).Trim()
        };
        entity.NormalizedContentKey = StringHelper.Normalize(entity.ContentKey);
        entity.CorrelationId = Optional(correlationId, nameof(CorrelationId), CustomerContentNormalizedRequestConsts.CorrelationIdMaxLength);
        entity.Update(status, currentMilestone, scrapingStatus, outlineStatus, retryCount, lastError);
        return entity;
    }

    public void Update(string status, string currentMilestone, string? scrapingStatus, string? outlineStatus, int retryCount, string? lastError)
    {
        Status = Guard.NotNullOrWhiteSpace(status, nameof(Status), CustomerContentNormalizedRequestConsts.StatusMaxLength).Trim();
        CurrentMilestone = Guard.NotNullOrWhiteSpace(currentMilestone, nameof(CurrentMilestone), CustomerContentNormalizedRequestConsts.CurrentMilestoneMaxLength).Trim();
        ScrapingStatus = Optional(scrapingStatus, nameof(ScrapingStatus), CustomerContentNormalizedRequestConsts.ScrapingStatusMaxLength);
        OutlineStatus = Optional(outlineStatus, nameof(OutlineStatus), CustomerContentNormalizedRequestConsts.OutlineStatusMaxLength);
        RetryCount = retryCount;
        LastError = Optional(lastError, nameof(LastError), CustomerContentNormalizedRequestConsts.LastErrorMaxLength);
    }

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);
}
