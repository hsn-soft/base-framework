using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;

public class HeyGenVideoRequest
{
    [JsonProperty("caption")]
    public bool? Caption { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("test")]
    public string Test { get; set; }

    [JsonProperty("template_id")]
    public string TemplateId { get; set; }

    [JsonProperty("callback_id")]
    public string CallbackId { get; set; }

    [JsonProperty("video_inputs")]
    public VideoInput VideoInputs { get; set; }

    [JsonProperty("dimension")]
    public Dimension Dimension { get; set; }

    [JsonProperty("callback_url")]
    public string CallbackUrl { get; set; }

    [JsonProperty("variables")]
    public dynamic Variables { get; set; }


}

public sealed class HeyGenVideoResponse
{
    [JsonProperty("data")]
    public HeyGenVideoResponseData Data { get; set; }

    [JsonProperty("error")]
    public HeyGenResponseError Error { get; set; }
}

public class HeyGenVideoResponseData
{
    [JsonProperty("video_id")]
    public string VideoId { get; set; }
}

public sealed class HeyGenVideoErrorResponse
{
    [JsonProperty("error")]
    public HeyGenResponseError Error { get; set; }
}

public sealed class HeyGenResponseError
{
    [JsonProperty("code")]
    public string Error { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

public sealed class HeyGenVideoQueryResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("data")]
    public HeyGenVideoQueryResponseData Data { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

public class HeyGenVideoQueryResponseData
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("video_url")]
    public string VideoUrl { get; set; }

    [JsonProperty("video_url_caption")]
    public string VideoUrlCaption { get; set; }

    [JsonProperty("callback_id")]
    public string CallBackId { get; set; }

    [JsonProperty("duration")]
    public float? Duration { get; set; }

    [JsonProperty("thumbnail_url")]
    public string ThumbnailUrl { get; set; }

    [JsonProperty("created_at")]
    public float? CreatedAt { get; set; }
}