using Newtonsoft.Json;

namespace OpenAI.Test.Console;

public class OpenAIErrorResponse
{
    [JsonProperty("error")]
    public OpenAIError Error { get; set; }
}

public class OpenAIError
{
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("param")]
    public string Param { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }
}