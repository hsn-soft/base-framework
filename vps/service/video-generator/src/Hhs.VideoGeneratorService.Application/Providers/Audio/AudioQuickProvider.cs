using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;

namespace Hhs.VideoGeneratorService.Application.Providers.Audio;

public sealed class AudioQuickProvider : IAudioProvider
{
    private readonly HttpClient httpClient;
    private readonly string _baseUrl;

    public AudioQuickProvider(HttpClient httpClient, AudioFastProviderSettings audioSettings, AudioPollingSettings pollingSettings)
    {
        this.httpClient = httpClient;
        this.httpClient.Timeout = TimeSpan.FromSeconds(pollingSettings.TimeoutSeconds);
        _baseUrl = audioSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.AudioQuick;

    public AudioProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.ImmediateResult };

    public async Task<AudioCreateResponse> CreateAsync(AudioCreateRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{_baseUrl}/audio/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new AudioCreateResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"AudioQuick generation failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string fileUrl = json.GetProperty("remoteFileUrl").GetString();
            string fileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

            return new AudioCreateResponse { IsCompleted = true, ProviderFileUrl = fileUrl, FileName = fileName };
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
                return new AudioStatusResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"AudioQuick status query failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            return new AudioStatusResponse { IsProcessed = true, ProviderFileUrl = json.GetProperty("remoteFileUrl").GetString(), FileName = json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null };
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