using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;

namespace Hhs.TextNormalizerService.Application.Providers.Outline;

public sealed class OutlineQueueProvider : IOutlineProvider
{
    private readonly HttpClient httpClient;
    private readonly string _baseUrl;

    public OutlineQueueProvider(HttpClient httpClient, OutlineQueueProviderSettings outlineSettings, OutlinePollingSettings pollingSettings)
    {
        this.httpClient = httpClient;
        this.httpClient.Timeout = TimeSpan.FromSeconds(pollingSettings.TimeoutSeconds);
        _baseUrl = outlineSettings == null ? throw new ArgumentNullException(nameof(outlineSettings)) : outlineSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.OutlineQueue;

    public OutlineProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling };

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
                return new OutlineCreateResponse { IsProcessFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"OutlineQueue generation failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string trackingId = json.GetProperty("trackingId").GetString();

            return new OutlineCreateResponse { IsProcessed = false, ProviderTrackId = trackingId };
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

    public async Task<OutlineStatusResponse> GetStatusAsync(OutlineStatusRequest request)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"{_baseUrl}/outline/status/{request.ProviderTrackId}"
            );

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new OutlineStatusResponse { IsProcessFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"OutlineQueue status query failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string status = json.GetProperty("status").GetString();

            if (status != "completed")
            {
                if (status != "failed")
                {
                    return new OutlineStatusResponse { IsProcessed = false };
                }

                string error = json.GetProperty("error").GetString();
                return new OutlineStatusResponse { IsProcessed = false, IsProcessFailed = true, IsRetryable = false, ErrorMessage = error };
            }

            string script = json.GetProperty("script").GetString();
            return new OutlineStatusResponse { IsProcessed = true, OutlinedData = script };
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new OutlineStatusResponse { IsProcessFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new OutlineStatusResponse { IsProcessFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }
}