using System.Net.Http.Json;
using System.Text.Json;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Domain.Constants;

namespace Hhs.VideoGeneratorService.Application.Providers.Video;

public sealed class VideoQueueExternalProvider : IVideoProvider
{
    private readonly HttpClient httpClient;
    private readonly string _baseUrl;

    public VideoQueueExternalProvider(HttpClient httpClient, VideoQueueExternalProviderSettings videoSettings, VideoPollingSettings pollingSettings)
    {
        this.httpClient = httpClient;
        this.httpClient.Timeout = TimeSpan.FromSeconds(pollingSettings.TimeoutSeconds);
        _baseUrl = videoSettings.BaseUrl;
    }

    public string ProviderKey => ProviderKeys.VideoQueueExternal;

    public VideoProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling, AudioInputMode = VideoAudioInputMode.AudioUrlListRequired };

    public async Task<VideoCreateResponse> CreateAsync(VideoCreateRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{_baseUrl}/video/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new VideoCreateResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"VideoQueueExternal generation failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string trackingId = json.GetProperty("trackingId").GetString();

            return new VideoCreateResponse { IsCompleted = false, ProviderTrackId = trackingId };
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new VideoCreateResponse { IsFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new VideoCreateResponse { IsFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<VideoStatusResponse> GetStatusAsync(string providerTrackId)
    {
        try
        {
            var response = await httpClient.GetAsync($"{_baseUrl}/video/status/{providerTrackId}");

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new VideoStatusResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"VideoQueueExternal status query failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            string status = json.GetProperty("status").GetString();

            if (status != ProviderStatusConstants.Completed)
            {
                if (status != ProviderStatusConstants.Failed)
                {
                    return new VideoStatusResponse { IsProcessed = false };
                }

                string error = json.GetProperty("error").GetString();
                return new VideoStatusResponse { IsProcessed = false, IsFailed = true, IsRetryable = false, ErrorMessage = error };
            }

            string fileUrl = status == ProviderStatusConstants.Completed ? json.GetProperty("remoteFileUrl").GetString() : null;
            string fileName = status == ProviderStatusConstants.Completed && json.TryGetProperty("fileName", out var fnProp) ? fnProp.GetString() : null;

            return new VideoStatusResponse { IsProcessed = true, IsFailed = false, ProviderFileUrl = fileUrl, FileName = fileName };
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new VideoStatusResponse { IsFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new VideoStatusResponse { IsFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }
}