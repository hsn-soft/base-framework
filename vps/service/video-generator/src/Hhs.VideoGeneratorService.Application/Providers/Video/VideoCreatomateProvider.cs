#nullable enable
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using HsnSoft.Base.Text;

namespace Hhs.VideoGeneratorService.Application.Providers.Video;

public sealed class VideoCreatomateProvider(
    HttpClient httpClient,
    VideoCreatomateProviderSettings videoSettings
) : IVideoProvider
{
    private const int MaxSlotCount = 5;

    private static readonly JsonSerializerOptions s_requestJsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public string ProviderKey => ProviderKeys.VideoCreatomate;

    public VideoProviderCapabilities Capabilities => new() { ProviderKey = ProviderKey, ExecutionMode = ProviderExecutionMode.AsyncPolling, AudioInputMode = VideoAudioInputMode.AudioUrlListRequired };

    public async Task<VideoCreateResponse> CreateAsync(VideoCreateRequest request)
    {
        if (request.CustomerProviderSettings is not ClientCreatomateSettings clientSettings)
            return new VideoCreateResponse { IsFailed = true, IsRetryable = false, ErrorMessage = "Creatomate requires per-customer ClientCreatomateSettings (CustomerVpSetting.VideoGenerationProviderSettings)." };

        string templateId = request.RefContentType == ContentType.AnalysisContent
            ? clientSettings.AnalysisVideoTemplateId
            : clientSettings.DirectVideoTemplateId;

        if (string.IsNullOrWhiteSpace(templateId))
            return new VideoCreateResponse { IsFailed = true, IsRetryable = false, ErrorMessage = $"Creatomate template id is not configured for RefContentType={request.RefContentType}." };

        try
        {
            var items = ParseVideoInputItems(request.VideoInputJson);
            var audioCdnUrls = request.AudioCdnUrls ?? [];

            var modifications = new CreatomateModifications
            {
                VideoWidth = clientSettings.VideoWidth,
                VideoHeight = clientSettings.VideoHeight,
                JenerikStartSource = clientSettings.JenericUrl,
                JenerikEndSource = clientSettings.JenericUrl,
                LogoSource = clientSettings.LogoUrl
            };

            int slotCount = Math.Min(items.Count, MaxSlotCount);
            for (int i = 0; i < slotCount; i++)
            {
                string? audioCdnUrl = i < audioCdnUrls.Count ? audioCdnUrls[i] : null;
                ApplySlot(modifications, i, audioCdnUrl, items[i].ImageUrl, items[i].Title, clientSettings.BackgroundColor);
            }

            var payload = new CreatomateVideoRequest { TemplateId = templateId, Modifications = modifications };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{videoSettings.BaseUrl}/v2/renders");
            httpRequest.Content = JsonContent.Create(payload, options: s_requestJsonOptions);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", videoSettings.ApiKey);

            using var response = await httpClient.SendAsync(httpRequest);
            string resJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new VideoCreateResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"Creatomate video generation failed: HTTP {(int)response.StatusCode} {resJson}" };
            }

            var result = JsonSerializer.Deserialize<CreatomateRenderResponse>(resJson);

            return new VideoCreateResponse { IsCompleted = false, ProviderTrackId = result?.Id };
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
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{videoSettings.BaseUrl}/v2/renders/{providerTrackId}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", videoSettings.ApiKey);

            using var response = await httpClient.SendAsync(httpRequest);
            string resJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new VideoStatusResponse { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"Creatomate status query failed: HTTP {(int)response.StatusCode} {resJson}" };
            }

            var result = JsonSerializer.Deserialize<CreatomateRenderResponse>(resJson);

            if (result?.Status == "succeeded")
                return new VideoStatusResponse { IsProcessed = true, ProviderFileUrl = result.Url };

            if (result?.Status == "failed")
                return new VideoStatusResponse { IsFailed = true, IsRetryable = false, ErrorMessage = result.ErrorMessage ?? "Creatomate render failed." };

            return new VideoStatusResponse { IsProcessed = false, IsFailed = false };
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

    private static void ApplySlot(CreatomateModifications m, int index, string? audioCdnUrl, string? imageUrl, string? text, string? backgroundColor)
    {
        switch (index)
        {
            case 0:
                m.Audio1Source = audioCdnUrl;
                m.Image1Source = imageUrl;
                m.Text1Text = text;
                m.Shape1FillColor = backgroundColor;
                m.Number1BackgroundColor = backgroundColor;
                break;
            case 1:
                m.Audio2Source = audioCdnUrl;
                m.Image2Source = imageUrl;
                m.Text2Text = text;
                m.Shape2FillColor = backgroundColor;
                m.Number2BackgroundColor = backgroundColor;
                break;
            case 2:
                m.Audio3Source = audioCdnUrl;
                m.Image3Source = imageUrl;
                m.Text3Text = text;
                m.Shape3FillColor = backgroundColor;
                m.Number3BackgroundColor = backgroundColor;
                break;
            case 3:
                m.Audio4Source = audioCdnUrl;
                m.Image4Source = imageUrl;
                m.Text4Text = text;
                m.Shape4FillColor = backgroundColor;
                m.Number4BackgroundColor = backgroundColor;
                break;
            case 4:
                m.Audio5Source = audioCdnUrl;
                m.Image5Source = imageUrl;
                m.Text5Text = text;
                m.Shape5FillColor = backgroundColor;
                m.Number5BackgroundColor = backgroundColor;
                break;
        }
    }

    private static List<CreatomateInputItem> ParseVideoInputItems(string videoInputJson)
    {
        using var doc = JsonDocument.Parse(videoInputJson);

        return doc.RootElement
            .GetProperty("audioItems")
            .EnumerateArray()
            .Select(x => new CreatomateInputItem
            {
                Title = x.TryGetProperty("encodedTitle", out var titleEl) && titleEl.GetString() is { } encodedTitle
                    ? StringHelper.Base64Decode(encodedTitle)
                    : null,
                ImageUrl = x.TryGetProperty("encodedImageUrl", out var imageEl) && imageEl.GetString() is { } encodedImageUrl
                    ? StringHelper.Base64Decode(encodedImageUrl)
                    : null
            })
            .ToList();
    }

    private sealed class CreatomateInputItem
    {
        public string? Title { get; set; }
        public string? ImageUrl { get; set; }
    }
}

internal sealed class CreatomateVideoRequest
{
    [JsonPropertyName("template_id")] public string? TemplateId { get; set; }

    [JsonPropertyName("modifications")] public CreatomateModifications? Modifications { get; set; }
}

internal sealed class CreatomateModifications
{
    [JsonPropertyName("width")] public short VideoWidth { get; set; }

    [JsonPropertyName("height")] public short VideoHeight { get; set; }

    [JsonPropertyName("Jenerik-Start.source")]
    public string? JenerikStartSource { get; set; }

    [JsonPropertyName("Jenerik-End.source")]
    public string? JenerikEndSource { get; set; }

    [JsonPropertyName("Logo.source")] public string? LogoSource { get; set; }

    [JsonPropertyName("Audio1.source")] public string? Audio1Source { get; set; }

    [JsonPropertyName("Image1.source")] public string? Image1Source { get; set; }

    [JsonPropertyName("Text1.text")] public string? Text1Text { get; set; }

    [JsonPropertyName("Shape1.fill_color")]
    public string? Shape1FillColor { get; set; }

    [JsonPropertyName("Number1.background_color")]
    public string? Number1BackgroundColor { get; set; }

    [JsonPropertyName("Audio2.source")] public string? Audio2Source { get; set; }

    [JsonPropertyName("Image2.source")] public string? Image2Source { get; set; }

    [JsonPropertyName("Text2.text")] public string? Text2Text { get; set; }

    [JsonPropertyName("Shape2.fill_color")]
    public string? Shape2FillColor { get; set; }

    [JsonPropertyName("Number2.background_color")]
    public string? Number2BackgroundColor { get; set; }

    [JsonPropertyName("Audio3.source")] public string? Audio3Source { get; set; }

    [JsonPropertyName("Image3.source")] public string? Image3Source { get; set; }

    [JsonPropertyName("Text3.text")] public string? Text3Text { get; set; }

    [JsonPropertyName("Shape3.fill_color")]
    public string? Shape3FillColor { get; set; }

    [JsonPropertyName("Number3.background_color")]
    public string? Number3BackgroundColor { get; set; }

    [JsonPropertyName("Audio4.source")] public string? Audio4Source { get; set; }

    [JsonPropertyName("Image4.source")] public string? Image4Source { get; set; }

    [JsonPropertyName("Text4.text")] public string? Text4Text { get; set; }

    [JsonPropertyName("Shape4.fill_color")]
    public string? Shape4FillColor { get; set; }

    [JsonPropertyName("Number4.background_color")]
    public string? Number4BackgroundColor { get; set; }

    [JsonPropertyName("Audio5.source")] public string? Audio5Source { get; set; }

    [JsonPropertyName("Image5.source")] public string? Image5Source { get; set; }

    [JsonPropertyName("Text5.text")] public string? Text5Text { get; set; }

    [JsonPropertyName("Shape5.fill_color")]
    public string? Shape5FillColor { get; set; }

    [JsonPropertyName("Number5.background_color")]
    public string? Number5BackgroundColor { get; set; }
}

/// <summary>
/// Creatomate's render-create (POST /v2/renders) and status-query (GET /v2/renders/{id}) responses
/// share the exact same shape (the create call returns the render's initial state), so one DTO
/// covers both — status is "planned"/"waiting"/"transcribing"/"rendering" while in progress,
/// "succeeded" with a populated Url on completion, or "failed" with ErrorMessage populated
/// (e.g. "A file could not be downloaded: &lt;url&gt; (element Audio1)" when a source asset,
/// like a locally-hosted CDN url unreachable from Creatomate's cloud, can't be fetched).
/// </summary>
internal sealed class CreatomateRenderResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("error_message")] public string? ErrorMessage { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("template_id")] public string? TemplateId { get; set; }

    [JsonPropertyName("template_name")] public string? TemplateName { get; set; }

    [JsonPropertyName("output_format")] public string? OutputFormat { get; set; }
}