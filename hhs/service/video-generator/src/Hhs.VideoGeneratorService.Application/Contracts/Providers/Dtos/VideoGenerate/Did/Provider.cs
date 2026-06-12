using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;

public class Provider
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("voice_id")]
    public string VoiceId { get; set; }

    [JsonProperty("model_id")]
    public string ModelId { get; set; }
}