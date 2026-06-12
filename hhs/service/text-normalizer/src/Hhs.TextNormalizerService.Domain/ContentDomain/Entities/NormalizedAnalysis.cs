using System.Globalization;
using Hhs.TextNormalizerService.Domain.ContentDomain.Consts;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Entities;

public sealed class NormalizedAnalysis : CreationAuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }

    public Guid TenantId { get; private set; }

    public Guid ClientId { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public Guid AnalysisContentId { get; private set; }
    public DateTime AnalysisDate { get; private set; }

    public NormalizedAnalysisStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    [NotNull] public List<AnalysisReferenceModel> AnalysisReferenceList { get; private set; }

    [CanBeNull] public string CorrelationId { get; set; }

    private NormalizedAnalysis()
    {
        AnalysisReferenceList = new List<AnalysisReferenceModel>();
    }

    internal NormalizedAnalysis(Guid id, Guid tenantId, Guid clientId, [NotNull] string domainName, Guid analysisContentId, DateTime analysisDate,
        [NotNull] List<AnalysisReferenceModel> analysisReferenceList,
        NormalizedAnalysisStates operationStatus, [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        TenantId = tenantId;
        ClientId = clientId;
        SetDomainName(domainName);

        AnalysisContentId = analysisContentId;
        SetAnalysisDate(analysisDate);

        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;

        SetAnalysisReferenceList(analysisReferenceList);
        CorrelationId = correlationId;
    }

    private void SetDomainName(string domainName)
    {
        DomainName = Check.NotNull(domainName, nameof(domainName), NormalizedAnalysisConsts.DomainNameMaxLength).ToLower(new CultureInfo("en-US"));
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
            if (analysisContent.AppContentId == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(analysisContent.AppContentId)} is invalid", nameof(analysisContent.AppContentId));
            }

            if (analysisContent.NormalizedRequestId == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(analysisContent.NormalizedRequestId)} is invalid", nameof(analysisContent.NormalizedRequestId));
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
    public Guid AppContentId { get; set; }

    public Guid NormalizedRequestId { get; set; }

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