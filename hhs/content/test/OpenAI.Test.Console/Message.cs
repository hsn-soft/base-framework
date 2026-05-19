using Newtonsoft.Json;

namespace OpenAI.Test.Console;

public class Message
{
    [JsonProperty("role")]
    public string role;

    [JsonProperty("content")]
    public string content;
}