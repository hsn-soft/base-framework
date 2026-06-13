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

public sealed class AnalysisNormalizedRequest : CreationAuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public Guid AnalysisContentId { get; private set; }
    public DateTime AnalysisDate { get; private set; }

    public AnalysisNormalizedRequestStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    [NotNull] public List<AnalysisReferenceModel> AnalysisReferenceList { get; private set; }

    [CanBeNull] public string CorrelationId { get; set; }

    private AnalysisNormalizedRequest()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        AnalysisReferenceList = [];
    }

    internal AnalysisNormalizedRequest(Guid id, [NotNull] string scopeKey, [NotNull] string domainName, Guid analysisContentId, DateTime analysisDate,
        [NotNull] List<AnalysisReferenceModel> analysisReferenceList,
        AnalysisNormalizedRequestStates operationStatus, [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetScopeKey(scopeKey);
        SetDomainName(domainName);

        AnalysisContentId = analysisContentId;
        SetAnalysisDate(analysisDate);

        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;

        SetAnalysisReferenceList(analysisReferenceList);
        CorrelationId = correlationId;
    }
    private void SetScopeKey(string scopeKey)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            scopeKey,
            $"{nameof(AnalysisNormalizedRequest)}:{nameof(ScopeKey)}",
            AnalysisNormalizedRequestConsts.ScopeKeyMaxLength
        );

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(AnalysisNormalizedRequestConsts)}:{nameof(DomainName)}", AnalysisNormalizedRequestConsts.DomainNameMaxLength);
        DomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomainName));
    }

    private void SetAnalysisDate(DateTime analysisDate)
    {
        if (analysisDate == default || analysisDate.ToUniversalTime().Date > DateTime.UtcNow.Date)
        {
            throw new ArgumentException($"{nameof(analysisDate)} is invalid", nameof(analysisDate));
        }

        AnalysisDate = analysisDate.ToUniversalTime().Date;
    }

    private void SetAnalysisReferenceList([NotNull] List<AnalysisReferenceModel> analysisReferenceList)
    {
        if (analysisReferenceList is { Count: < 1 })
        {
            throw new ArgumentException($"{nameof(AnalysisReferenceList)} is invalid", nameof(analysisReferenceList));
        }

        foreach (var analysisContent in analysisReferenceList)
        {
            if (analysisContent.CustomerContentId == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(analysisContent.CustomerContentId)} is invalid", nameof(analysisContent.CustomerContentId));
            }

            if (analysisContent.ContentNormalizedRequestId == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(analysisContent.ContentNormalizedRequestId)} is invalid", nameof(analysisContent.ContentNormalizedRequestId));
            }

            if (string.IsNullOrWhiteSpace(analysisContent.AnalysisDataModel?.Title))
            {
                throw new ArgumentException($"{nameof(analysisContent.AnalysisDataModel.Title)} is invalid", nameof(analysisContent.AnalysisDataModel.Title));
            }
        }

        AnalysisReferenceList = analysisReferenceList;
    }
}

public sealed class AnalysisReferenceModel
{
    public Guid CustomerContentId { get; set; }

    public Guid ContentNormalizedRequestId { get; set; }

    [NotNull] public AnalysisDataModel AnalysisDataModel { get; set; }

    [CanBeNull] public string OutlineContentData { get; set; }
}

public sealed class AnalysisDataModel
{
    [NotNull] public string Title { get; set; }

    [CanBeNull] public string Spot { get; set; }

    [CanBeNull] public string ImageUrl { get; set; }

    [CanBeNull] public string Details { get; set; }
}