using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;

public class ColossyanVideoGenerationRequest
{
    [JsonProperty("videoCreative")]
    public VideoCreative VideoCreative{ get; set; }
}

public class ColossyanVideoGenerationResponse
{
    [JsonProperty("id")]
    public string Id{ get; set; }
}