using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AnalysisContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    // Soft Delete
    public bool IsDeleted { get; internal set; }

    // Subscription & Scope
    [NotNull] public string ScopeKey { get; private set; }

    public DateTime AnalysisDate { get; private set; }

    // Correlation & Tracing
    [CanBeNull] public string CorrelationId { get; private set; }
    [CanBeNull] public string LastFacility { get; set; }
    [CanBeNull] public string LastError { get; set; }

    // Normalization Status
    [CanBeNull] public string NormalizeStatus { get; set; }
    [CanBeNull] public Guid? NormalizeRequestId { get; set; }

    // Video Generation Status
    [CanBeNull] public string VideoStatus { get; set; }
    [CanBeNull] public Guid? VideoRequestId { get; set; }
    [CanBeNull] public string VideoCdnUrl { get; set; }

    // Analysis Items
    public List<AnalysisContentItem> Items { get; private set; } = [];

    private AnalysisContent()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
    }

    public AnalysisContent([NotNull] string scopeKey, DateTime analysisDate, [CanBeNull] string correlationId = null)
        : this(id: Guid.CreateVersion7(), scopeKey: scopeKey, analysisDate: analysisDate, correlationId: correlationId)
    {
    }

    public AnalysisContent(Guid id, [NotNull] string scopeKey, DateTime analysisDate, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetScopeKey(scopeKey);
        SetAnalysisDate(analysisDate);
        CorrelationId = correlationId;
    }

    private void SetScopeKey(string scopeKey)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            scopeKey,
            $"{nameof(CustomerContent)}:{nameof(ScopeKey)}",
            AnalysisContentConsts.ScopeKeyMaxLength
        );

    private void SetAnalysisDate(DateTime analysisDate)
    {
        if (analysisDate == default || analysisDate.ToUniversalTime().Date > DateTime.UtcNow.Date)
        {
            throw new ArgumentException($"{nameof(AnalysisDate)} is invalid", nameof(analysisDate));
        }

        AnalysisDate = analysisDate.ToUniversalTime().Date;
    }
}