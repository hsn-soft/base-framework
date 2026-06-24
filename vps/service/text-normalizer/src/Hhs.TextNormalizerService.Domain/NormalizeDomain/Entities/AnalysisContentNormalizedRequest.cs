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
}