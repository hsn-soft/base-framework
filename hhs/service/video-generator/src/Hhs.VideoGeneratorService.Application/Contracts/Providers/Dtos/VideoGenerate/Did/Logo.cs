using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class Logo
{
    [JsonProperty("position")]
    public int[] Position { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}