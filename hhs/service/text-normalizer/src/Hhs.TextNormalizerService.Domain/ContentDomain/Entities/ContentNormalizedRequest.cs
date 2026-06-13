using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Entities;

public sealed class ContentNormalizedRequest : CreationAuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public Guid CustomerContentId { get; private set; }

    [NotNull] public string DomainPath { get; private set; }

    public ContentNormalizedRequestStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    [CanBeNull] public ScrapingContentDataModel ScrapingContentData { get; private set; }

    [CanBeNull] public string OutlineContentData { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }

    private ContentNormalizedRequest()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        DomainName = string.Empty;
        DomainPath = string.Empty;
    }

    internal ContentNormalizedRequest(Guid id, [NotNull] string scopeKey, [NotNull] string domainName, Guid customerContentId, [NotNull] string domainPath,
        ContentNormalizedRequestStates operationStatus, [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] ScrapingContentDataModel scrapingContentData = null, [CanBeNull] string outlineContentData = null, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetScopeKey(scopeKey);
        SetDomainName(domainName);
        CustomerContentId = customerContentId;

        SetDomainPath(domainPath);

        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;

        SetScrapingContentData(scrapingContentData);
        OutlineContentData = outlineContentData;
        CorrelationId = correlationId;
    }

    private void SetScopeKey(string scopeKey)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            scopeKey,
            $"{nameof(ContentNormalizedRequest)}:{nameof(ScopeKey)}",
            ContentNormalizedRequestConsts.ScopeKeyMaxLength
        );

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(ContentNormalizedRequest)}:{nameof(DomainName)}", ContentNormalizedRequestConsts.DomainNameMaxLength);
        DomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomainName));
    }

    private void SetDomainPath(string domainPath)
    {
        DomainPath = StringHelper.Minimize(Check.NotNull(domainPath, nameof(domainPath), ContentNormalizedRequestConsts.DomainPathMaxLength));
    }

    internal void SetScrapingContentData([CanBeNull] ScrapingContentDataModel scrapingContentData)
    {
        if (scrapingContentData is { ReleaseTime: not null } && (scrapingContentData.ReleaseTime.Value == default))
        {
            throw new ArgumentException($"{nameof(scrapingContentData.ReleaseTime)} is invalid", nameof(scrapingContentData.ReleaseTime));
        }

        if (scrapingContentData != null)
        {
            scrapingContentData.ReleaseTime = scrapingContentData.ReleaseTime?.ToUniversalTime();
        }

        ScrapingContentData = scrapingContentData;
    }
}

public sealed class ScrapingContentDataModel
{
    [NotNull] public string Title { get; set; }

    public DateTime? ReleaseTime { get; set; }

    [CanBeNull] public string Spot { get; set; }

    [CanBeNull] public string Details { get; set; }

    [CanBeNull] public string ImageUrl { get; set; }
}