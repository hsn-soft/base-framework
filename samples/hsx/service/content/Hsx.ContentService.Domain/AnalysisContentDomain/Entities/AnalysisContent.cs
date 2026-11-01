using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.ContentService.Domain.AnalysisContentDomain.Consts;

namespace Hsx.ContentService.Domain.AnalysisContentDomain.Entities;

/// <summary>
/// The analysis of the contents of a customer on a day: a video made of the best of them. It is seen by its scope key.
/// </summary>
public sealed class AnalysisContent : FullAuditedEntity<Guid>, IScopeSubscription
{
    // for EF Core
    private AnalysisContent()
    {
    }

    public string ScopeKey { get; private set; } = string.Empty;

    public DateTimeOffset AnalysisDate { get; private set; }

    public string? NormalizeStatus { get; private set; }

    public string? VideoStatus { get; private set; }

    public string? VideoCdnUrl { get; private set; }

    public string? CurrentMilestone { get; private set; }

    public string? LastError { get; private set; }

    public static AnalysisContent Create(string scopeKey, DateTimeOffset analysisDate, string? normalizeStatus, string? videoStatus, string? videoCdnUrl, string? currentMilestone, string? lastError)
    {
        AnalysisContent entity = new()
        {
            Id = Guid.CreateVersion7(),
            ScopeKey = Guard.NotNullOrWhiteSpace(scopeKey, nameof(ScopeKey), AnalysisContentConsts.ScopeKeyMaxLength).Trim(),
            AnalysisDate = analysisDate
        };
        entity.Update(normalizeStatus, videoStatus, videoCdnUrl, currentMilestone, lastError);
        return entity;
    }

    public void Update(string? normalizeStatus, string? videoStatus, string? videoCdnUrl, string? currentMilestone, string? lastError)
    {
        NormalizeStatus = Optional(normalizeStatus, nameof(NormalizeStatus), AnalysisContentConsts.NormalizeStatusMaxLength);
        VideoStatus = Optional(videoStatus, nameof(VideoStatus), AnalysisContentConsts.VideoStatusMaxLength);
        VideoCdnUrl = Optional(videoCdnUrl, nameof(VideoCdnUrl), AnalysisContentConsts.VideoCdnUrlMaxLength);
        CurrentMilestone = Optional(currentMilestone, nameof(CurrentMilestone), AnalysisContentConsts.CurrentMilestoneMaxLength);
        LastError = Optional(lastError, nameof(LastError), AnalysisContentConsts.LastErrorMaxLength);
    }

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);
}
