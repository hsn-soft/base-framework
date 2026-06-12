using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class VideoCreative
{
    [JsonProperty("settings")]
    public Settings Settings{ get; set; }

    [JsonProperty("scenes")]
    public List<Scene> Scenes{ get; set; }
}