using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.ContentService.Domain.CustomerContentDomain.Consts;

namespace Hsx.ContentService.Domain.CustomerContentDomain.Entities;

/// <summary>
/// A content of a customer (a page of its site), made into a video: its keys, the state of its normalization and of its video. It is seen by its scope key.
/// </summary>
public sealed class CustomerContent : FullAuditedEntity<Guid>, IScopeSubscription
{
    // for EF Core
    private CustomerContent()
    {
    }

    public string ScopeKey { get; private set; } = string.Empty;

    public string ContentKey { get; private set; } = string.Empty;
    public string NormalizedContentKey { get; private set; } = string.Empty;

    public string SlugKey { get; private set; } = string.Empty;

    public string? NormalizeStatus { get; private set; }

    public string? VideoStatus { get; private set; }

    public string? VideoCdnUrl { get; private set; }

    public string? CurrentMilestone { get; private set; }

    public string? LastError { get; private set; }

    public static CustomerContent Create(string scopeKey, string contentKey, string slugKey, string? normalizeStatus, string? videoStatus, string? videoCdnUrl, string? currentMilestone, string? lastError)
    {
        CustomerContent entity = new()
        {
            Id = Guid.CreateVersion7(),
            ScopeKey = Guard.NotNullOrWhiteSpace(scopeKey, nameof(ScopeKey), CustomerContentConsts.ScopeKeyMaxLength).Trim(),
            ContentKey = Guard.NotNullOrWhiteSpace(contentKey, nameof(ContentKey), CustomerContentConsts.ContentKeyMaxLength).Trim()
        };
        entity.NormalizedContentKey = StringHelper.Normalize(entity.ContentKey);
        entity.Update(slugKey, normalizeStatus, videoStatus, videoCdnUrl, currentMilestone, lastError);
        return entity;
    }

    public void Update(string slugKey, string? normalizeStatus, string? videoStatus, string? videoCdnUrl, string? currentMilestone, string? lastError)
    {
        SlugKey = Guard.NotNullOrWhiteSpace(slugKey, nameof(SlugKey), CustomerContentConsts.SlugKeyMaxLength).Trim();
        NormalizeStatus = Optional(normalizeStatus, nameof(NormalizeStatus), CustomerContentConsts.NormalizeStatusMaxLength);
        VideoStatus = Optional(videoStatus, nameof(VideoStatus), CustomerContentConsts.VideoStatusMaxLength);
        VideoCdnUrl = Optional(videoCdnUrl, nameof(VideoCdnUrl), CustomerContentConsts.VideoCdnUrlMaxLength);
        CurrentMilestone = Optional(currentMilestone, nameof(CurrentMilestone), CustomerContentConsts.CurrentMilestoneMaxLength);
        LastError = Optional(lastError, nameof(LastError), CustomerContentConsts.LastErrorMaxLength);
    }

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);
}
