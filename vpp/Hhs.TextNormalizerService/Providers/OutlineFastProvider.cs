using Hhs.Shared.Providers;
using Hhs.TextNormalizerService.Configuration;
using Hhs.TextNormalizerService.Configuration.Providers.Outline;
using System.Text.Json;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineFastProvider(HttpClient httpClient, OutlineFastProviderSettings outlineSettings) : IOutlineProvider
{
    private readonly string _baseUrl = outlineSettings == null ? throw new ArgumentNullException(nameof(outlineSettings)) : outlineSettings.BaseUrl;

    public string ProviderKey => ProviderKeys.OutlineFast;

    public OutlineProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.ImmediateResult };

    public async Task<OutlineCreateResponse> CreateAsync(OutlineCreateRequest request, CancellationToken cancellationToken)
    {
        var inputText = string.IsNullOrWhiteSpace(request.OutlineInput) ? request.OutlinePrompt : request.OutlineInput;
        if (string.IsNullOrWhiteSpace(inputText))
            inputText = "Default outline content";

        var mockRequest = new { InputText = inputText };
        var response = await httpClient.PostAsJsonAsync(
            $"{_baseUrl}/outline/generate",
            mockRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        string? script = json.GetProperty("script").GetString();

        return new OutlineCreateResponse { IsProcessed = true, OutlinedData = script };
    }

    public Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{ProviderKey} outline provider does not support tracking.");
}