using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class Position
{
    [JsonProperty("x")]
    public string? X { get; set; }

    [JsonProperty("x")]
    public string? Y { get; set; }
}