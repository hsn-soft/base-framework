using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;

public class BackgroundSettings
{
    [JsonProperty("text")]
    public string Type { get; set;}
}

public class ColorBackground : BackgroundSettings
{
    [JsonProperty("value")]
    public string Value { get; set; }
}

public class ImageBackground : BackgroundSettings
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("image_asset_id")]
    public string ImageAssetId { get; set; }

    [JsonProperty("fit")]
    public string Fit { get; set; } //cover , crop, contain and none
}

public class VideoBackground : BackgroundSettings
{
    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("video_asset_id")]
    public string VideoAssetId { get; set; }

    [JsonProperty("play_style")]
    public string PlayStyle { get; set; } //fit_to_scene, freeze, loop, once

    [JsonProperty("fit")]
    public string Fit { get; set; } //cover , crop, contain and none
}
