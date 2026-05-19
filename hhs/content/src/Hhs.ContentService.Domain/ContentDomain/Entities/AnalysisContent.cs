using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AnalysisContent : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }

    public Guid TenantId { get; private set; }

    public Guid ClientId { get; private set; }

    [CanBeNull]
    public Client Client { get; } = null;


    public DateTime AnalysisDate { get; private set; }

    public AnalysisContentOperationStates OperationStatus { get; internal set; }

    [CanBeNull]
    public string OperationStatusDescription { get; internal set; }

    public Guid? NormalizedAnalysisId { get; private set; }

    public Guid? VideoRequestId { get; private set; }

    [CanBeNull]
    public string StorageVideoUrl { get; internal set; }

    [CanBeNull]
    public string CorrelationId { get; internal set; }

    private AnalysisContent()
    {
        AnalysisDate = DateTime.UtcNow.Date;
    }

    internal AnalysisContent(Guid id, Guid tenantId, Guid clientId, DateTime analysisDate,
        AnalysisContentOperationStates operationStatus, [CanBeNull] string operationStatusDescription = null,
        Guid? normalizedAnalysisId = null, Guid? videoRequestId = null, [CanBeNull] string storageVideoUrl = null,
        [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetTenantId(tenantId);
        SetClientId(clientId);
        SetAnalysisDate(analysisDate);

        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;

        SetNormalizedAnalysisId(normalizedAnalysisId);

        SetVideoRequestId(videoRequestId);
        StorageVideoUrl = storageVideoUrl;
        CorrelationId = correlationId;
    }

    internal void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(TenantId)} is invalid", nameof(tenantId));
        }

        TenantId = tenantId;
    }

    internal void SetClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
        }

        ClientId = clientId;
    }

    internal void SetAnalysisDate(DateTime analysisDate)
    {
        if (analysisDate == default || analysisDate.ToUniversalTime().Date > DateTime.UtcNow.Date)
        {
            throw new ArgumentException($"{nameof(AnalysisDate)} is invalid", nameof(analysisDate));
        }

        AnalysisDate = analysisDate.ToUniversalTime().Date;
    }

    internal void SetNormalizedAnalysisId(Guid? normalizedAnalysisId)
    {
        if (normalizedAnalysisId.HasValue && normalizedAnalysisId.Value == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(NormalizedAnalysisId)} is invalid", nameof(normalizedAnalysisId));
        }

        NormalizedAnalysisId = normalizedAnalysisId;
    }

    internal void SetVideoRequestId(Guid? videoRequestId)
    {
        if (videoRequestId.HasValue && videoRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(VideoRequestId)} is invalid", nameof(videoRequestId));
        }

        VideoRequestId = videoRequestId;
    }
}