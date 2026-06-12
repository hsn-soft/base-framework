using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class VideoSize
{
    [JsonProperty("height")]
    public string? Height { get; set; }

    [JsonProperty("width")]
    public string? Width { get; set; }
}