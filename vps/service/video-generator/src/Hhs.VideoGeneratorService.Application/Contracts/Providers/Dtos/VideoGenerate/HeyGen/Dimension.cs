using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;
public class Dimension
{
    [JsonProperty("width")]
    public int Width { get; set; }
    [JsonProperty("height")]
    public int Height { get; set; }
}