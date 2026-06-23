using Hhs.FeedRService.Domain.ReportingDomain.Enums;
using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.FeedRService.Domain.ReportingDomain.Entities;

/// <summary>
/// Tracks the derivation from a single MongoDB <c>RawGoogleAdManagerResponse</c> document
/// to zero or more PostgreSQL <see cref="DashboardResponse"/> records. Serves as the
/// audit trail and idempotency key for the MongoDB → PostgreSQL pipeline.
/// </summary>
public sealed class MongoToPostgresMapping : AuditedEntity<Guid>
{
    public Guid MongoDbDocumentId { get; private set; }

    /// <summary>CSV of derived DailyReportResponse Ids (keeps the migration simple — JSON not required).</summary>
    public string DerivedRecordIds { get; private set; } = string.Empty;

    public int DerivedRecordCount { get; private set; }

    public DateTime DerivationDate { get; private set; }

    public DerivationStatus DerivationStatus { get; private set; }

    public string ErrorMessage { get; private set; }

    private MongoToPostgresMapping() { }

    public MongoToPostgresMapping(
        Guid id,
        Guid mongoDbDocumentId,
        IEnumerable<Guid> derivedRecordIds,
        DerivationStatus status,
        string errorMessage = null)
    {
        Id = id;
        MongoDbDocumentId = mongoDbDocumentId;
        var ids = derivedRecordIds?.ToArray() ?? Array.Empty<Guid>();
        DerivedRecordIds = string.Join(',', ids);
        DerivedRecordCount = ids.Length;
        DerivationDate = DateTime.UtcNow;
        DerivationStatus = status;
        ErrorMessage = errorMessage;
    }
}
