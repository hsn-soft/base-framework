using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class Track
{

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("actor")]
    public string? Actor { get; set; }

    [JsonProperty("position")]
    public Position Position { get; set; }

    [JsonProperty("size")]
    public VideoSize Size { get; set; }

    [JsonProperty("texzt")]
    public string? Text { get; set; }

    [JsonProperty("speakerId")]
    public string? SpeakerId { get; set; }

    [JsonProperty("removeBackground")]
    public string? RemoveBackground { get; set; }
}