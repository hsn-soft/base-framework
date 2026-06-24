using Hhs.FeedRService.Domain.ReportingDomain.Enums;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.FeedRService.Domain.ReportingDomain.Entities;

/// <summary>
/// MongoDB bulk storage of raw Google Ad Manager report responses.
/// Stores the complete NDJSON response body plus a structured parsed form
/// as a list of <see cref="GoogleAdManagerReportRow"/> items in a single document.
/// </summary>
public sealed class RawGoogleAdManagerResponse : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid TenantId { get; private set; }
    public Guid ClientId { get; private set; }
    [NotNull]
    public string ClientName { get; private set; } = string.Empty;

    /// <summary>Unique correlation identifier linking this response to a CreateReportRequest / job run.</summary>
    [NotNull]
    public string RequestId { get; private set; } = string.Empty;

    /// <summary>Job name (e.g. GenerateSummaryReport) that triggered this response.</summary>
    [NotNull]
    public string JobName { get; private set; } = string.Empty;

    /// <summary>Google Ad Manager network the report was executed against.</summary>
    [NotNull]
    public string Network { get; private set; } = string.Empty;

    /// <summary>The AdUnitIdTopLevel used as the report filter.</summary>
    [NotNull]
    public string AdUnitIdTopLevel { get; private set; } = string.Empty;

    /// <summary>The AdUnitId used as the report filter (the client-specific ad unit).</summary>
    [NotNull]
    public string AdUnitId { get; private set; } = string.Empty;

    /// <summary>The date the report data is representing (Google Ad Manager Yesterday relative range).</summary>
    public DateTime ReportDate { get; private set; }

    /// <summary>When the response was received from Google Ad Manager.</summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>The raw (verbatim) NDJSON response body returned by Google Ad Manager.</summary>
    [NotNull]
    public string RawResponse { get; private set; } = string.Empty;

    /// <summary>The response parsed into structured rows - stored as a list within the single document.</summary>
    public List<GoogleAdManagerReportRow> Rows { get; private set; } = new();

    /// <summary>Current derivation status to PostgreSQL.</summary>
    public DerivationStatus ProcessedStatus { get; private set; } = DerivationStatus.Pending;

    public DateTime? ProcessedAt { get; private set; }
    [CanBeNull]
    public string ProcessingError { get; private set; }

    private RawGoogleAdManagerResponse() { }

    public RawGoogleAdManagerResponse(
        Guid id,
        Guid tenantId,
        string requestId,
        string jobName,
        string network,
        string adUnitIdTopLevel,
        string adUnitId,
        DateTime reportDate,
        string rawResponse,
        List<GoogleAdManagerReportRow> rows)
    {
        Id = id;
        TenantId = tenantId;
        RequestId = requestId ?? string.Empty;
        JobName = jobName ?? string.Empty;
        Network = network ?? string.Empty;
        AdUnitIdTopLevel = adUnitIdTopLevel ?? string.Empty;
        AdUnitId = adUnitId ?? string.Empty;
        ReportDate = reportDate.Date;
        Timestamp = DateTime.UtcNow;
        RawResponse = rawResponse ?? string.Empty;
        Rows = rows ?? new List<GoogleAdManagerReportRow>();
        ProcessedStatus = DerivationStatus.Pending;
    }

    public void MarkProcessing() => ProcessedStatus = DerivationStatus.Processing;

    public void MarkCompleted()
    {
        ProcessedStatus = DerivationStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        ProcessingError = null;
    }

    public void MarkFailed(string error)
    {
        ProcessedStatus = DerivationStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
        ProcessingError = error;
    }
}
