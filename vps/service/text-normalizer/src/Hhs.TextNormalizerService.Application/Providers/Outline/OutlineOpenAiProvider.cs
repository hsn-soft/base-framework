using Hhs.Shared.Helper.Providers;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Hhs.TextNormalizerService.Application.Providers.Outline;

public sealed class OutlineOpenAiProvider(OutlineOpenAiProviderSettings settings) : IOutlineProvider
{
    public string ProviderKey => ProviderKeys.OutlineOpenAi;

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public async Task<OutlineCreateResponse> OutlineOperationAsync(OutlineCreateRequest request)
    {
        var inputText = string.IsNullOrWhiteSpace(request.OutlineInput) ? request.OutlinePrompt : request.OutlineInput;
        if (string.IsNullOrWhiteSpace(request.OutlinePrompt) || string.IsNullOrWhiteSpace(inputText))
            return new OutlineCreateResponse { IsProcessed = false, IsProcessFailed = true, ErrorMessage = "INVALID_OUTLINE_REQUEST" };

        if (string.IsNullOrWhiteSpace(settings.APIKey))
            return new OutlineCreateResponse { IsProcessed = false, IsProcessFailed = true, ErrorMessage = "OUTLINE_OPENAI_API_KEY_MISSING" };

        var model = string.IsNullOrWhiteSpace(request.EngineModel) ? settings.Engine : request.EngineModel;
        var credential = new ApiKeyCredential(settings.APIKey);

        ChatClient client;
        if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            var openAiClient = new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = new Uri(settings.BaseUrl) });
            client = openAiClient.GetChatClient(model ?? "gpt-4o-mini");
        }
        else
        {
            client = new ChatClient(model ?? "gpt-4o-mini", credential);
        }

        try
        {
            ChatCompletion completion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(request.OutlinePrompt),
                new UserChatMessage(inputText)
            ]);

            var text = completion.Content[0].Text
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ");

            return new OutlineCreateResponse { IsProcessed = true, OutlinedData = text };
        }
        catch (Exception ex)
        {
            return new OutlineCreateResponse { IsProcessed = false, IsProcessFailed = true, ErrorMessage = ex.Message };
        }
    }

    public Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request)
        => throw new NotSupportedException($"{ProviderKey} does not support async status tracking.");
}
