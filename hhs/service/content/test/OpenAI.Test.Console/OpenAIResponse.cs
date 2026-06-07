using Newtonsoft.Json;

namespace OpenAI.Test.Console;

public class OpenAIResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; }

    [JsonProperty("created")]
    public long Created { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("choices")]
    public List<Choice> Choices { get; set; }

    [JsonProperty("usage")]
    public Usage Usage { get; set; }
}

public class Choice
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("message")]
    public Message Message { get; set; }

    [JsonProperty("logprobs")]
    public Logprobs Logprobs { get; set; }

    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
}

public class Logprobs
{
    [JsonProperty("tokens")]
    public List<string> Tokens { get; set; }

    [JsonProperty("token_logprobs")]
    public List<double?> TokenLogprobs { get; set; }

    [JsonProperty("top_logprobs")]
    public IList<IDictionary<string, double>> TopLogprobs { get; set; }

    [JsonProperty("text_offset")]
    public List<int> TextOffsets { get; set; }
}

public class Usage
{
    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}
