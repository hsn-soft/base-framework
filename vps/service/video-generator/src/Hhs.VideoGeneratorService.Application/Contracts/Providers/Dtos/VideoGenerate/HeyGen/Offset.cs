using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;
public class Offset
{
    [JsonProperty("x")]
    public float X { get; set; }
    [JsonProperty("y")]
    public float Y { get; set; }
}