using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;
using Hhs.VideoGeneratorService.Domain.Constants;

namespace Hhs.VideoGeneratorService.Application.Providers.Audio;

public sealed class AudioHqProvider : IAudioProvider
{
    private readonly HttpClient httpClient;
    private readonly string _baseUrl;

    public AudioHqProvider(HttpClient httpClient, AudioQueueProviderSettings audioSettings, AudioPollingSettings pollingSettings)
    {
        this.httpClient = httpClient;
        this.httpClient.Timeout = TimeSpan.FromSeconds(pollingSettings.TimeoutSeconds);
        _baseUrl = audioSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.AudioHQ;

    public AudioProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling };

    public async Task<AudioCreateResponse> CreateAsync(AudioCreateRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{_baseUrl}/audio/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new AudioCreateResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"AudioHQ generation failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string trackingId = json.GetProperty("trackingId").GetString();

            return new AudioCreateResponse { IsCompleted = false, ProviderTrackId = trackingId };
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new AudioCreateResponse { IsFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new AudioCreateResponse { IsFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<AudioStatusResponse> GetStatusAsync(string providerTrackId)
    {
        try
        {
            var response = await httpClient.GetAsync($"{_baseUrl}/audio/status/{providerTrackId}");

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new AudioStatusResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"AudioHQ status query failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string status = json.GetProperty("status").GetString();

            if (status != ProviderStatusConstants.Completed)
            {
                if (status != ProviderStatusConstants.Failed)
                {
                    return new AudioStatusResponse { IsProcessed = false };
                }

                string error = json.GetProperty("error").GetString();
                // The remote provider itself reported a terminal failure for this job — not a
                // transport-level error we can classify by HTTP status, so treat as non-retryable.
                return new AudioStatusResponse { IsProcessed = false, IsFailed = true, IsRetryable = false, ErrorMessage = error };
            }

            string fileUrl = status == ProviderStatusConstants.Completed ? json.GetProperty("remoteFileUrl").GetString() : null;
            string fileName = status == ProviderStatusConstants.Completed && json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

            return new AudioStatusResponse { IsProcessed = true, IsFailed = false, ProviderFileUrl = fileUrl, FileName = fileName };
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new AudioStatusResponse { IsFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new AudioStatusResponse { IsFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }
}