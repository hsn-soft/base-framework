using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;

namespace Hhs.TextNormalizerService.Application.Providers.Outline;

public sealed class OutlineFastProvider : IOutlineProvider
{
    private readonly HttpClient httpClient;
    private readonly string _baseUrl;

    public OutlineFastProvider(HttpClient httpClient, OutlineFastProviderSettings outlineSettings, OutlinePollingSettings pollingSettings)
    {
        this.httpClient = httpClient;
        this.httpClient.Timeout = TimeSpan.FromSeconds(pollingSettings.TimeoutSeconds);
        _baseUrl = outlineSettings == null ? throw new ArgumentNullException(nameof(outlineSettings)) : outlineSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.OutlineFast;

    public OutlineProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.ImmediateResult };

    public async Task<OutlineCreateResponse> OutlineOperationAsync(OutlineCreateRequest request)
    {
        try
        {
            string inputText = string.IsNullOrWhiteSpace(request.OutlineInput) ? request.OutlinePrompt : request.OutlineInput;
            if (string.IsNullOrWhiteSpace(inputText))
                inputText = "Default outline content";

            var mockRequest = new { InputText = inputText };
            var response = await httpClient.PostAsJsonAsync(
                $"{_baseUrl}/outline/generate",
                mockRequest
            );

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new OutlineCreateResponse { IsProcessFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"OutlineFast generation failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string script = json.GetProperty("script").GetString();

            return new OutlineCreateResponse { IsProcessed = true, OutlinedData = script };
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new OutlineCreateResponse { IsProcessFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new OutlineCreateResponse { IsProcessFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }

    public Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request)
        => throw new NotSupportedException($"{ProviderKey} outline provider does not support tracking.");
}