using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AnalysisContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    public DateTime AnalysisDate { get; set; }

    public AnalysisContentOperationStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    public Guid? NormalizedRequestId { get; set; }

    public Guid? VideoRequestId { get; set; }

    [CanBeNull] public string StorageVideoUrl { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }


    private AnalysisContent()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        AnalysisDate = DateTime.UtcNow.Date;
    }

    internal AnalysisContent(Guid customerId, ProductTypes productType, DateTime analysisDate,
        AnalysisContentOperationStates operationStatus, [CanBeNull] string correlationId = null)
        : this(Guid.CreateVersion7(), customerId, productType, analysisDate, operationStatus, correlationId)
    {
    }

    internal AnalysisContent(Guid id, Guid customerId, ProductTypes productType, DateTime analysisDate,
        AnalysisContentOperationStates operationStatus, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetScopeKey(customerId, productType);

        SetAnalysisDate(analysisDate);

        OperationStatus = operationStatus;
        CorrelationId = correlationId;
    }

    private void SetScopeKey(Guid customerId, ProductTypes productType)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            ScopeKeyHelper.Generate(customerId, productType),
            $"{nameof(CustomerContent)}:{nameof(ScopeKey)}",
            AnalysisContentConsts.ScopeKeyMaxLength
        );

    internal void SetAnalysisDate(DateTime analysisDate)
    {
        if (analysisDate == default || analysisDate.ToUniversalTime().Date > DateTime.UtcNow.Date)
        {
            throw new ArgumentException($"{nameof(AnalysisDate)} is invalid", nameof(analysisDate));
        }

        AnalysisDate = analysisDate.ToUniversalTime().Date;
    }
}