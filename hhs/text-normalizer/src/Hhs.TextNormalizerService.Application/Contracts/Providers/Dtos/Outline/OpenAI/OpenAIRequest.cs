using Newtonsoft.Json;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAI;

public class OpenAIRequest
{
    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("messages")]
    public List<Message> Messages { get; set; }

    [JsonProperty("temperature")]
    public float Temperature { get; set; }

    [JsonProperty("n")]
    public int N { get; set; }
    /*

    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonProperty("presence_penalty")]
    public int PresencePenalty { get; set; }

    // public int top_p;
    */
    public OpenAIRequest(string model, string prompt)
    {
        Model = model;
        Messages = new List<Message> { new() { role = "user", content = prompt } };
        Temperature = 0.7f;
        N = 1;
        //MaxTokens = 50;
        //PresencePenalty = 2;
    }
}