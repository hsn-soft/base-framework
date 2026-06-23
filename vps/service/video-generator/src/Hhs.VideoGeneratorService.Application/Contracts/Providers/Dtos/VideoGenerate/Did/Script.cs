using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class Script
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("subtitles")]
    public string Subtitles { get; set; }

    [JsonProperty("input")]
    public string Input { get; set; }

    [JsonProperty("ssml")]
    public string Ssml { get; set; }

    [JsonProperty("provider")]
    public Provider Provider { get; set; }

    [JsonProperty("audio_url")]
    public string AudioUrl { get; set; }
}