using Newtonsoft.Json;

namespace YepicAI.Test.Console;

public class TalkingPhotoRequest
{
    [JsonProperty("parentId")]
    public string? ParentId { get; set; }

    [JsonProperty("groupId")]
    public string? GroupId { get; set; }

    [JsonProperty("draft")]
    public bool Draft { get; set; }

    [JsonProperty("avatarId")]
    public string AvatarId { get; set; }

    [JsonProperty("avatarName")]
    public string? AvatarName { get; set; }

    [JsonProperty("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [JsonProperty("fullFrame")]
    public bool FullFrame { get; set; }

    [JsonProperty("dynamic")]
    public bool Dynamic { get; set; }

    [JsonProperty("voiceId")]
    public string VoiceId { get; set; }

    [JsonProperty("voiceOverId")]
    public string? VoiceOverId { get; set; }

    [JsonProperty("voiceOverName")]
    public string? VoiceOverName { get; set; }

    [JsonProperty("voiceOverUrl")]
    public string? VoiceOverUrl { get; set; }

    [JsonProperty("script")]
    public string Script { get; set; }

    [JsonProperty("videoFormat")]
    public string VideoFormat { get; set; }

    [JsonProperty("videoWidth")]
    public int VideoWidth { get; set; }

    [JsonProperty("videoHeight")]
    public int VideoHeight { get; set; }

    [JsonProperty("videoTitle")]
    public string? VideoTitle { get; set; }

    [JsonProperty("visibility")]
    public string Visibility { get; set; }
}

public sealed class TalkingPhotoResponse : TalkingPhotoRequest
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("storageProvider")]
    public string StorageProvider { get; set; }

    [JsonProperty("userId")]
    public string UserId { get; set; }

    [JsonProperty("dateCreated")]
    public DateTimeOffset? DateCreated { get; set; }

    [JsonProperty("renderDuration")]
    public TimeSpan? RenderDuration { get; set; }

    [JsonProperty("requestDuration")]
    public TimeSpan? RequestDuration { get; set; }

    public DateTimeOffset? DateRenderCompleted => DateCreated.HasValue && RenderDuration.HasValue
        ? DateCreated.Value.Add(RenderDuration.Value)
        : null;

    [JsonProperty("renderProgress")]
    public string? RenderProgress { get; set; }

    [JsonProperty("videoPreviewImageUrl")]
    public string? VideoPreviewImageUrl { get; set; }

    [JsonProperty("videoUrl")]
    public string? VideoUrl { get; set; }

    [JsonProperty("customVideoUrl")]
    public bool CustomVideoUrl { get; set; }

    [JsonProperty("videoWatermarkedUrl")]
    public string? VideoWatermarkedUrl { get; set; }

    [JsonProperty("customVideoWatermarkedUrl")]
    public bool CustomVideoWatermarkedUrl { get; set; }

    [JsonProperty("videoLength")]
    public string? VideoLength { get; set; }

    [JsonProperty("removeBackground")]
    public bool RemoveBackground { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("backgroundColor")]
    public string? BackgroundColor { get; set; }

    [JsonProperty("backgroundImageId")]
    public string? BackgroundImageId { get; set; }

    [JsonProperty("backgroundImageName")]
    public string? BackgroundImageName { get; set; }

    [JsonProperty("backgroundImageUrl")]
    public string? BackgroundImageUrl { get; set; }

    [JsonProperty("backgroundVideoId")]
    public string? BackgroundVideoId { get; set; }

    [JsonProperty("backgroundVideoName")]
    public string? BackgroundVideoName { get; set; }

    [JsonProperty("backgroundVideoUrl")]
    public string? BackgroundVideoUrl { get; set; }

    [JsonProperty("speechVolume")]
    public string? SpeechVolume { get; set; }

    [JsonProperty("speechSpeed")]
    public string? SpeechSpeed { get; set; }
}