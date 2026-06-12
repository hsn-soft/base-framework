using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class PresenterConfig
{
    [JsonProperty("crop")]
    public Crop Crop{ get; set; }
}