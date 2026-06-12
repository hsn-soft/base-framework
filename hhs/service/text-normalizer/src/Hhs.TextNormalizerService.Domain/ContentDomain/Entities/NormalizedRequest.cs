using System.Globalization;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Entities;

public sealed class NormalizedRequest : CreationAuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }

    public Guid TenantId { get; private set; }

    public Guid ClientId { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public Guid AppContentId { get; private set; }

    [NotNull] public string DomainPath { get; private set; }

    public NormalizedRequestStates OperationStatus { get;  set; }

    [CanBeNull] public string OperationStatusDescription { get;  set; }

    [CanBeNull] public ScrapingContentDataModel ScrapingContentData { get; private set; }

    [CanBeNull] public string OutlineContentData { get;  set; }

    [CanBeNull] public string CorrelationId { get;  set; }

    private NormalizedRequest()
    {
        DomainName = string.Empty;
        DomainPath = string.Empty;
    }

    internal NormalizedRequest(Guid id, Guid tenantId, Guid clientId, [NotNull] string domainName, Guid appContentId, [NotNull] string domainPath,
        NormalizedRequestStates operationStatus, [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] ScrapingContentDataModel scrapingContentData = null, [CanBeNull] string outlineContentData = null, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        TenantId = tenantId;
        ClientId = clientId;
        SetDomainName(domainName);
        AppContentId = appContentId;

        SetDomainPath(domainPath);

        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;

        SetScrapingContentData(scrapingContentData);
        OutlineContentData = outlineContentData;
        CorrelationId = correlationId;
    }

    private void SetDomainName(string domainName)
    {
        DomainName = Check.NotNull(domainName, nameof(domainName), NormalizedRequestConsts.DomainNameMaxLength).ToLower(new CultureInfo("en-US"));
    }

    private void SetDomainPath(string domainPath)
    {
        DomainPath = Check.NotNull(domainPath, nameof(domainPath), NormalizedRequestConsts.DomainPathMaxLength).ToLower(new CultureInfo("en-US"));
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