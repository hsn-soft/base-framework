using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class Settings
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("videoSize")]
    public VideoSize VideoSize{ get; set; }
}