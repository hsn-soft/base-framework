using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class Background
{
    [JsonProperty("source_url")]
    public string SourceUrl { get; set; }
}