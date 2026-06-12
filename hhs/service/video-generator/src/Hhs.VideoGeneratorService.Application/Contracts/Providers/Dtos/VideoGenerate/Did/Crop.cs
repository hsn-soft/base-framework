using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class Crop
{
    [JsonProperty("type")]
    public string Type { get; set; }
}