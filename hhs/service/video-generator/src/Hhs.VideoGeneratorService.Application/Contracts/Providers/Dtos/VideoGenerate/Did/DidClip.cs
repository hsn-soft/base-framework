using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class DidClipRequest
{
    [JsonProperty("presenter_id")]
    public string PresenterId { get; set; }

    [JsonProperty("driver_id")]
    public string DriverId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("user_data")]
    [CanBeNull]
    public string UserData { get; set; }

    [JsonProperty("script")]
    public Script Script { get; set; }

    [JsonProperty("background")]
    public Background Background { get; set; }

    [JsonProperty("presenter_config")]
    public PresenterConfig PresenterConfig { get; set; }

    [JsonProperty("config")]
    public Config Config { get; set; }

    [JsonProperty("result_url")]
    public string ResultUrl{ get; set; }
}

public sealed class DidClipResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("result_url")]
    public string ResultUrl { get; set; }
}

public sealed class DidClipErrorResponse
{
    [JsonProperty("message")]
    public string ErrorMessage { get; set; }
}