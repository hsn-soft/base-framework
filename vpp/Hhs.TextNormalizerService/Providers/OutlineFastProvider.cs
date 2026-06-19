using Hhs.Shared.Providers;
using Hhs.TextNormalizerService.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Hhs.TextNormalizerService.Providers;

public sealed class OutlineFastProvider : IOutlineProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public string ProviderKey => ProviderKeys.OutlineFast;

    public OutlineProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.ImmediateResult
    };

    public OutlineFastProvider(HttpClient httpClient, IOptions<OutlineProviderEndpointsOptions> options)
    {
        _httpClient = httpClient;
        _baseUrl = options.Value.FastBaseUrl;
    }

    public async Task<OutlineCreateResponse> CreateAsync(
        OutlineCreateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/outline/generate",
            request,
            cancellationToken);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var script = json.GetProperty("script").GetString();

        return new OutlineCreateResponse
        {
            IsCompleted = true,
            Script = script
        };
    }

    public Task<OutlineStatusResponse> GetStatusAsync(
        string providerTrackId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException("OpenAI outline provider does not support polling.");
    }
}