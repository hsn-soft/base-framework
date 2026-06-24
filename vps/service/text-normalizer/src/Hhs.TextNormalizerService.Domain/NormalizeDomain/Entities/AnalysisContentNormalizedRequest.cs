using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using HsnSoft.Base.Domain.Entities.Auditing;
using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;

public sealed class AnalysisContentNormalizedRequest : AuditedEntity<Guid>
{

    // Correlation
    public string? CorrelationId { get; set; }
    public Guid SourceEventId { get; set; }

    // Subscription & Scope
    public string ScopeKey { get; set; } = default!;

    // Content Reference
    public Guid AnalysisContentId { get; set; }
    public string DomainName { get; set; } = default!;

    // Status & Progress
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    // Analysis Items
    public List<AnalysisNormalizedItem> Items { get; set; } = [];

    // Error Handling
    public string? LastError { get; set; }

    private AnalysisContentNormalizedRequest() { }

    public AnalysisContentNormalizedRequest(Guid id, string scopeKey, Guid analysisContentId, string domainName, Guid? correlationId = null)
    {
        Id = id;
        ScopeKey = scopeKey;
        AnalysisContentId = analysisContentId;
        DomainName = domainName;
        CorrelationId = correlationId?.ToString();
    }
}