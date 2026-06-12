using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;

public class InnerVariable
{
    [JsonProperty("name")]
    public string Name { get; set;}

    [JsonProperty("type")]
    public string Type { get; set;}

    [JsonProperty("properties")]
    public Properties Properties { get; set;}


}