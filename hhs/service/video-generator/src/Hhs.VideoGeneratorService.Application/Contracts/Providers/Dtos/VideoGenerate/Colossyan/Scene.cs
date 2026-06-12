using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class Scene
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("tracks")]
    public List<Track> Tracks { get; set; }
}