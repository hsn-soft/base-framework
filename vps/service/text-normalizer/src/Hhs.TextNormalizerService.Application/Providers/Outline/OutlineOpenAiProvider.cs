using Hhs.Shared.Helper.Providers;
using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAIResponses;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hhs.TextNormalizerService.Application.Providers.Outline;

public sealed class OutlineOpenAiProvider(OutlineOpenAiProviderSettings settings) : IOutlineProvider
{
    // Schema is equivalent to what JSchemaGenerator produces from StructuredOutput
    // with DefaultRequired = Required.Always and AllowAdditionalProperties = false
    private static readonly BinaryData s_structuredOutputSchema = BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "category": {
              "type": "array",
              "items": {
                "type": "string",
                "enum": ["Politika","Spor","Sanat","Egitim","Finans","Savas","Dunya","Yerel","Teknoloji","Magazin","HavaDurumu","Mizah","Eglence","Yasam","SonDakika","Saglik","Bilim","Seyahat","Kultur"]
              }
            },
            "tags":    { "type": "array",  "items": { "type": "string" } },
            "spot":    { "type": "string" },
            "title":   { "type": "string" },
            "summary": { "type": "string" }
          },
          "required": ["category", "tags", "spot", "title", "summary"],
          "additionalProperties": false
        }
        """);

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ProviderKey => ProviderKeys.OutlineOpenAi;

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public async Task<OutlineCreateResponse> OutlineOperationAsync(OutlineCreateRequest request)
    {
        string inputText = string.IsNullOrWhiteSpace(request.OutlineInput) ? request.OutlinePrompt : request.OutlineInput;
        if (string.IsNullOrWhiteSpace(request.OutlinePrompt) || string.IsNullOrWhiteSpace(inputText))
            return new OutlineCreateResponse { IsProcessed = false, IsProcessFailed = true, ErrorMessage = "INVALID_OUTLINE_REQUEST" };

        if (string.IsNullOrWhiteSpace(settings.APIKey))
            return new OutlineCreateResponse { IsProcessed = false, IsProcessFailed = true, ErrorMessage = "OUTLINE_OPENAI_API_KEY_MISSING" };

        string model = string.IsNullOrWhiteSpace(request.EngineModel) ? settings.Engine : request.EngineModel;
        ApiKeyCredential credential = new(settings.APIKey);

        ChatClient client;
        if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            OpenAIClient openAiClient = new(credential, new OpenAIClientOptions { Endpoint = new Uri(settings.BaseUrl) });
            client = openAiClient.GetChatClient(model ?? "gpt-4o-mini");
        }
        else
        {
            client = new ChatClient(model ?? "gpt-4o-mini", credential);
        }

        ChatMessage[] messages =
        [
            new SystemChatMessage(request.OutlinePrompt),
            new UserChatMessage(inputText)
        ];

        try
        {
            return request.UseStructuredOutput
                ? await OutlineWithStructuredOutputAsync(client, messages)
                : await OutlineSimpleAsync(client, messages);
        }
        catch (Exception ex)
        {
            return new OutlineCreateResponse { IsProcessed = false, IsProcessFailed = true, ErrorMessage = ex.Message };
        }
    }

    private static async Task<OutlineCreateResponse> OutlineSimpleAsync(ChatClient client, ChatMessage[] messages)
    {
        ChatCompletion completion = await client.CompleteChatAsync(messages);

        string text = completion.Content[0].Text
            .Replace("-", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ");

        return new OutlineCreateResponse { IsProcessed = true, OutlinedData = text };
    }

    private static async Task<OutlineCreateResponse> OutlineWithStructuredOutputAsync(ChatClient client, ChatMessage[] messages)
    {
        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "outline_response",
                s_structuredOutputSchema,
                jsonSchemaIsStrict: true
            )
        };

        ChatCompletion completion = await client.CompleteChatAsync(messages, options);
        string responseJson = completion.Content[0].Text;

        StructuredOutput structuredOutput = JsonSerializer.Deserialize<StructuredOutput>(responseJson, s_jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize OpenAI response to structured output.");

        List<string> categories = structuredOutput.Category?
            .Select(c => c.ToString())
            .ToList() ?? [];

        return new OutlineCreateResponse
        {
            IsProcessed = true,
            OutlinedData = structuredOutput.Summary,
            Categories = categories.Count > 0 ? categories : null,
            Tags = structuredOutput.Tags?.Count > 0 ? structuredOutput.Tags : null
        };
    }

    public Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request)
        => throw new NotSupportedException($"{ProviderKey} does not support async status tracking.");
}
