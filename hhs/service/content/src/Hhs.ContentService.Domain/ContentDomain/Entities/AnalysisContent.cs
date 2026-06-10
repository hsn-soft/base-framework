using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AnalysisContent : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    public Guid ClientId { get; set; }

    public DateTime AnalysisDate { get; set; }

    public AnalysisContentOperationStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    public Guid? NormalizedAnalysisId { get; set; }

    public Guid? VideoRequestId { get; set; }

    [CanBeNull] public string StorageVideoUrl { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }


    private AnalysisContent()
    {
        // Not-Null string fields
        AnalysisDate = DateTime.UtcNow.Date;
    }

    internal AnalysisContent(Guid tenantId, Guid clientId, DateTime analysisDate,
        AnalysisContentOperationStates operationStatus, [CanBeNull] string correlationId = null)
        : this(Guid.CreateVersion7(), tenantId, clientId, analysisDate, operationStatus, correlationId)
    {
    }

    internal AnalysisContent(Guid id, Guid tenantId, Guid clientId, DateTime analysisDate,
        AnalysisContentOperationStates operationStatus, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        TenantId = tenantId;
        ClientId = clientId;

        SetAnalysisDate(analysisDate);

        OperationStatus = operationStatus;
        CorrelationId = correlationId;
    }

    internal void SetAnalysisDate(DateTime analysisDate)
    {
        if (analysisDate == default || analysisDate.ToUniversalTime().Date > DateTime.UtcNow.Date)
        {
            throw new ArgumentException($"{nameof(AnalysisDate)} is invalid", nameof(analysisDate));
        }

        AnalysisDate = analysisDate.ToUniversalTime().Date;
    }
}