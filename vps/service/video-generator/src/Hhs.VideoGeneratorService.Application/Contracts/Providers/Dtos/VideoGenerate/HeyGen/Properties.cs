using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;

public class Properties
{
    [JsonProperty("url")]
    public string Url { get; set;}

    [JsonProperty("asset_id")]
    public string AssetId { get; set;}

    [JsonProperty("fit")]
    public string Fit { get; set;}

    [JsonProperty("play_style")]
    public string PlayStyle { get; set;}

    [JsonProperty("content")]
    public string Content { get; set;}
}