using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class Config
{
    [JsonProperty("logo")]
    public Logo Logo { get; set; }

    [JsonProperty("result_format")]
    public string ResultFormat { get; set; }

    [JsonProperty("output_resolution")]
    public int OutputResolution { get; set; }
}