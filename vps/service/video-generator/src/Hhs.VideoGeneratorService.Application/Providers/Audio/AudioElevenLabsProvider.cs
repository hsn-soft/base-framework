using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;
using Microsoft.Extensions.Hosting;

namespace Hhs.VideoGeneratorService.Application.Providers.Audio;

public sealed class AudioElevenLabsProvider(
    HttpClient httpClient,
    AudioElevenLabsProviderSettings audioSettings,
    SystemCdnSettings cdnSettings,
    IHostEnvironment environment
) : IAudioProvider
{
    private static readonly JsonSerializerOptions s_requestJsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public string ProviderKey => ProviderKeys.AudioElevenLabs;

    public AudioProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.ImmediateResult };

    public async Task<AudioCreateResponse> CreateAsync(AudioCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(audioSettings.ApiKey))
            return new AudioCreateResponse { IsFailed = true, IsRetryable = false, ErrorMessage = "ElevenLabs ApiKey is not configured." };

        if (string.IsNullOrWhiteSpace(audioSettings.VoiceId))
            return new AudioCreateResponse { IsFailed = true, IsRetryable = false, ErrorMessage = "ElevenLabs VoiceId is not configured." };

        var payload = new
        {
            text = request.InputText,
            model_id = audioSettings.Model,
            language_code = audioSettings.LanguageCode,
            voice_settings = new
            {
                stability = audioSettings.Stability,
                similarity_boost = audioSettings.SimilarityBoost,
                style = 0.0,
                use_speaker_boost = false,
                speed = audioSettings.Speed
            },
            apply_text_normalization = "on"
        };

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{audioSettings.BaseUrl}{audioSettings.VoiceId}");
            httpRequest.Content = JsonContent.Create(payload, options: s_requestJsonOptions);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
            httpRequest.Headers.Add("xi-api-key", audioSettings.ApiKey);

            using var response = await httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                return new AudioCreateResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"ElevenLabs audio generation failed: HTTP {(int)response.StatusCode} {errorBody}" };
            }

            // ElevenLabs returns the generated audio bytes directly in the response body — there is no
            // separately-hosted remote URL to hand back. Persist it to the same local directory
            // RemoteFileDownloader uses, and return that local path as ProviderFileUrl:
            // RemoteFileDownloader.DownloadAsync recognizes an already-local path and short-circuits
            // instead of attempting an HTTP GET against it.
            string downloadDir = cdnSettings.LocalDownloadPath;
            if (environment.IsDevelopment())
            {
                downloadDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", downloadDir);
            }

            Directory.CreateDirectory(downloadDir);

            string fileKey = string.IsNullOrWhiteSpace(request.AudioReferenceKey)
                ? Guid.CreateVersion7().ToString("N").ToLower()
                : request.AudioReferenceKey;

            string fileName = $"{fileKey}.mp3";
            string filePath = Path.Combine(downloadDir, fileName);

            await using (var contentStream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = File.Create(filePath))
            {
                await contentStream.CopyToAsync(fileStream);
            }

            return new AudioCreateResponse { IsCompleted = true, ProviderFileUrl = filePath, FileName = fileName };
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

    public Task<AudioStatusResponse> GetStatusAsync(string providerTrackId)
        => throw new NotSupportedException("ElevenLabs audio generation is synchronous (ImmediateResult); polling is not supported.");
}