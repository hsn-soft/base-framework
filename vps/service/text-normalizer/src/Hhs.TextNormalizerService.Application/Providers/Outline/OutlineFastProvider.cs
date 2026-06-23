using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;

namespace Hhs.TextNormalizerService.Application.Providers.Outline;

public sealed class OutlineFastProvider(HttpClient httpClient, OutlineFastProviderSettings outlineSettings) : IOutlineProvider
{
    private readonly string _baseUrl = outlineSettings == null ? throw new ArgumentNullException(nameof(outlineSettings)) : outlineSettings.BaseUrl;

    public string ProviderKey => ProviderKeys.OutlineFast;

    public OutlineProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.ImmediateResult };

    public async Task<OutlineCreateResponse> CreateAsync(OutlineCreateRequest request)
    {
        var inputText = string.IsNullOrWhiteSpace(request.OutlineInput) ? request.OutlinePrompt : request.OutlineInput;
        if (string.IsNullOrWhiteSpace(inputText))
            inputText = "Default outline content";

        var mockRequest = new { InputText = inputText };
        var response = await httpClient.PostAsJsonAsync(
            $"{_baseUrl}/outline/generate",
            mockRequest
            );

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? script = json.GetProperty("script").GetString();

        return new OutlineCreateResponse { IsProcessed = true, OutlinedData = script };
    }

    public Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request)
        => throw new NotSupportedException($"{ProviderKey} outline provider does not support tracking.");
}