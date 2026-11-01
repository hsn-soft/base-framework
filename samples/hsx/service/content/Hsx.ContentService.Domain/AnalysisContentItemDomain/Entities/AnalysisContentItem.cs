using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Consts;

namespace Hsx.ContentService.Domain.AnalysisContentItemDomain.Entities;

/// <summary>
/// A content in an analysis, in its place. It carries the scope key of the analysis (copied), so it is seen by its own key and not through its parent.
/// </summary>
public sealed class AnalysisContentItem : Entity<Guid>, IScopeSubscription
{
    // for EF Core
    private AnalysisContentItem()
    {
    }

    public string ScopeKey { get; private set; } = string.Empty;

    public Guid AnalysisContentId { get; private set; }

    public Guid CustomerContentId { get; private set; }

    public int SortOrder { get; private set; }

    public string? SourceTitle { get; private set; }

    public string? SourceDescription { get; private set; }

    public string? SourceImageUrl { get; private set; }

    public static AnalysisContentItem Create(string scopeKey, Guid analysisContentId, Guid customerContentId, int sortOrder, string? sourceTitle, string? sourceDescription, string? sourceImageUrl)
    {
        AnalysisContentItem entity = new()
        {
            Id = Guid.CreateVersion7(),
            ScopeKey = Guard.NotNullOrWhiteSpace(scopeKey, nameof(ScopeKey), AnalysisContentItemConsts.ScopeKeyMaxLength).Trim(),
            AnalysisContentId = Guard.NotDefault<Guid>(analysisContentId, nameof(AnalysisContentId)),
            CustomerContentId = Guard.NotDefault<Guid>(customerContentId, nameof(CustomerContentId))
        };
        entity.Update(sortOrder, sourceTitle, sourceDescription, sourceImageUrl);
        return entity;
    }

    public void Update(int sortOrder, string? sourceTitle, string? sourceDescription, string? sourceImageUrl)
    {
        SortOrder = sortOrder;
        SourceTitle = Optional(sourceTitle, nameof(SourceTitle), AnalysisContentItemConsts.SourceTitleMaxLength);
        SourceDescription = Optional(sourceDescription, nameof(SourceDescription), AnalysisContentItemConsts.SourceDescriptionMaxLength);
        SourceImageUrl = Optional(sourceImageUrl, nameof(SourceImageUrl), AnalysisContentItemConsts.SourceImageUrlMaxLength);
    }

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);
}
