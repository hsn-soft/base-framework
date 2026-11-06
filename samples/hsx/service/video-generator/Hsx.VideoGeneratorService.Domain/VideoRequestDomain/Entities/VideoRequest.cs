using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Consts;

namespace Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Entities;

/// <summary>
/// The request to make a video of a content: its input, the providers that make its audio and video, and where the video is stored. It is seen by its scope key.
/// </summary>
public sealed class VideoRequest : FullAuditedEntity<Guid>, IScopeSubscription
{
    // for the database driver
    private VideoRequest()
    {
    }

    public string ScopeKey { get; private set; } = string.Empty;

    public Guid RefContentId { get; private set; }

    /// <summary><c>CustomerContent</c> or <c>AnalysisContent</c>.</summary>
    public string RefContentType { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string CurrentMilestone { get; private set; } = string.Empty;

    public string MediaInputJson { get; private set; } = string.Empty;

    public string? AudioProviderKey { get; private set; }

    public string VideoProviderKey { get; private set; } = string.Empty;

    public string? VideoProviderTrackingId { get; private set; }

    public string? VideoProviderUrl { get; private set; }

    public string? VideoStorageUrl { get; private set; }

    public string? VideoCdnUrl { get; private set; }

    public int RetryCount { get; private set; }

    public string? LastError { get; private set; }

    public static VideoRequest Create(string scopeKey, Guid refContentId, string refContentType, string status, string currentMilestone, string mediaInputJson, string? audioProviderKey, string videoProviderKey, string? videoProviderTrackingId, string? videoProviderUrl, string? videoStorageUrl, string? videoCdnUrl, int retryCount, string? lastError)
    {
        VideoRequest entity = new()
        {
            Id = Guid.CreateVersion7(),
            ScopeKey = Guard.NotNullOrWhiteSpace(scopeKey, nameof(ScopeKey), VideoRequestConsts.ScopeKeyMaxLength).Trim(),
            RefContentId = Guard.NotDefault<Guid>(refContentId, nameof(RefContentId)),
            RefContentType = Guard.NotNullOrWhiteSpace(refContentType, nameof(RefContentType), VideoRequestConsts.RefContentTypeMaxLength).Trim()
        };
        entity.Update(status, currentMilestone, mediaInputJson, audioProviderKey, videoProviderKey, videoProviderTrackingId, videoProviderUrl, videoStorageUrl, videoCdnUrl, retryCount, lastError);
        return entity;
    }

    public void Update(string status, string currentMilestone, string mediaInputJson, string? audioProviderKey, string videoProviderKey, string? videoProviderTrackingId, string? videoProviderUrl, string? videoStorageUrl, string? videoCdnUrl, int retryCount, string? lastError)
    {
        Status = Guard.NotNullOrWhiteSpace(status, nameof(Status), VideoRequestConsts.StatusMaxLength).Trim();
        CurrentMilestone = Guard.NotNullOrWhiteSpace(currentMilestone, nameof(CurrentMilestone), VideoRequestConsts.CurrentMilestoneMaxLength).Trim();
        MediaInputJson = Guard.NotNullOrWhiteSpace(mediaInputJson, nameof(MediaInputJson), VideoRequestConsts.MediaInputJsonMaxLength).Trim();
        AudioProviderKey = Optional(audioProviderKey, nameof(AudioProviderKey), VideoRequestConsts.AudioProviderKeyMaxLength);
        VideoProviderKey = Guard.NotNullOrWhiteSpace(videoProviderKey, nameof(VideoProviderKey), VideoRequestConsts.VideoProviderKeyMaxLength).Trim();
        VideoProviderTrackingId = Optional(videoProviderTrackingId, nameof(VideoProviderTrackingId), VideoRequestConsts.VideoProviderTrackingIdMaxLength);
        VideoProviderUrl = Optional(videoProviderUrl, nameof(VideoProviderUrl), VideoRequestConsts.VideoProviderUrlMaxLength);
        VideoStorageUrl = Optional(videoStorageUrl, nameof(VideoStorageUrl), VideoRequestConsts.VideoStorageUrlMaxLength);
        VideoCdnUrl = Optional(videoCdnUrl, nameof(VideoCdnUrl), VideoRequestConsts.VideoCdnUrlMaxLength);
        RetryCount = retryCount;
        LastError = Optional(lastError, nameof(LastError), VideoRequestConsts.LastErrorMaxLength);
    }

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);
}
