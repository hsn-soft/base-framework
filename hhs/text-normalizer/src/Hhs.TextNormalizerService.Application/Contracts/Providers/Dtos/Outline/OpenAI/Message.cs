using Newtonsoft.Json;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAI;

public class Message
{
    [JsonProperty("role")]
    public string role;

    [JsonProperty("content")]
    public string content;
}