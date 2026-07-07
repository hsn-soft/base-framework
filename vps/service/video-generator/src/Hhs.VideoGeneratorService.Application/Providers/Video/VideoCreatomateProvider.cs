using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Video;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;

namespace Hhs.VideoGeneratorService.Application.Providers.Video;

public sealed class VideoCreatomateProvider : IVideoProvider
{
    private const int MaxSlotCount = 5;

    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly VideoCreatomateProviderSettings _settings;

    public VideoCreatomateProvider(HttpClient httpClient, VideoCreatomateProviderSettings videoSettings)
    {
        _httpClient = httpClient;
        _settings = videoSettings;
    }

    public string ProviderKey => ProviderKeys.VideoCreatomate;

    public VideoProviderCapabilities Capabilities => new()
    {
        ProviderKey = ProviderKey,
        ExecutionMode = ProviderExecutionMode.AsyncPolling,
        AudioInputMode = VideoAudioInputMode.AudioUrlListRequired
    };

    public async Task<VideoCreateResponse> CreateAsync(VideoCreateRequest request)
    {
        if (request.CustomerProviderSettings is not ClientCreatomateSettings clientSettings)
            throw new InvalidOperationException("Creatomate requires per-customer ClientCreatomateSettings (CustomerVpSetting.VideoGenerationProviderSettings).");

        string templateId = request.RefContentType == ContentType.AnalysisContent
            ? clientSettings.AnalysisVideoTemplateId
            : clientSettings.DirectVideoTemplateId;

        if (string.IsNullOrWhiteSpace(templateId))
            throw new InvalidOperationException($"Creatomate template id is not configured for RefContentType={request.RefContentType}.");

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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/renders")
        {
            Content = JsonContent.Create(payload, options: RequestJsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest);
        string resJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Creatomate video generation failed: HTTP {(int)response.StatusCode} {resJson}");

        var result = JsonSerializer.Deserialize<CreatomateVideoResponse>(resJson);

        return new VideoCreateResponse { IsCompleted = false, ProviderTrackId = result?.VideoId };
    }

    public async Task<VideoStatusResponse> GetStatusAsync(string providerTrackId)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/v2/renders/{providerTrackId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest);
        string resJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Creatomate status query failed: HTTP {(int)response.StatusCode} {resJson}");

        var result = JsonSerializer.Deserialize<CreatomateVideoQueryResponse>(resJson);

        if (result?.Status == "succeeded")
            return new VideoStatusResponse { IsCompleted = true, ProviderFileUrl = result.VideoUrl };

        if (result?.Status == "failed")
            return new VideoStatusResponse { IsFailed = true, ErrorMessage = "Creatomate render failed." };

        return new VideoStatusResponse { IsCompleted = false, IsFailed = false };
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
                Title = x.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null,
                ImageUrl = x.TryGetProperty("imageUrl", out var imageEl) ? imageEl.GetString() : null
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
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; } = default!;

    [JsonPropertyName("modifications")]
    public CreatomateModifications Modifications { get; set; } = default!;
}

internal sealed class CreatomateModifications
{
    [JsonPropertyName("width")]
    public short VideoWidth { get; set; }

    [JsonPropertyName("height")]
    public short VideoHeight { get; set; }

    [JsonPropertyName("Jenerik-Start.source")]
    public string? JenerikStartSource { get; set; }

    [JsonPropertyName("Jenerik-End.source")]
    public string? JenerikEndSource { get; set; }

    [JsonPropertyName("Logo.source")]
    public string? LogoSource { get; set; }

    [JsonPropertyName("Audio1.source")]
    public string? Audio1Source { get; set; }

    [JsonPropertyName("Image1.source")]
    public string? Image1Source { get; set; }

    [JsonPropertyName("Text1.text")]
    public string? Text1Text { get; set; }

    [JsonPropertyName("Shape1.fill_color")]
    public string? Shape1FillColor { get; set; }

    [JsonPropertyName("Number1.background_color")]
    public string? Number1BackgroundColor { get; set; }

    [JsonPropertyName("Audio2.source")]
    public string? Audio2Source { get; set; }

    [JsonPropertyName("Image2.source")]
    public string? Image2Source { get; set; }

    [JsonPropertyName("Text2.text")]
    public string? Text2Text { get; set; }

    [JsonPropertyName("Shape2.fill_color")]
    public string? Shape2FillColor { get; set; }

    [JsonPropertyName("Number2.background_color")]
    public string? Number2BackgroundColor { get; set; }

    [JsonPropertyName("Audio3.source")]
    public string? Audio3Source { get; set; }

    [JsonPropertyName("Image3.source")]
    public string? Image3Source { get; set; }

    [JsonPropertyName("Text3.text")]
    public string? Text3Text { get; set; }

    [JsonPropertyName("Shape3.fill_color")]
    public string? Shape3FillColor { get; set; }

    [JsonPropertyName("Number3.background_color")]
    public string? Number3BackgroundColor { get; set; }

    [JsonPropertyName("Audio4.source")]
    public string? Audio4Source { get; set; }

    [JsonPropertyName("Image4.source")]
    public string? Image4Source { get; set; }

    [JsonPropertyName("Text4.text")]
    public string? Text4Text { get; set; }

    [JsonPropertyName("Shape4.fill_color")]
    public string? Shape4FillColor { get; set; }

    [JsonPropertyName("Number4.background_color")]
    public string? Number4BackgroundColor { get; set; }

    [JsonPropertyName("Audio5.source")]
    public string? Audio5Source { get; set; }

    [JsonPropertyName("Image5.source")]
    public string? Image5Source { get; set; }

    [JsonPropertyName("Text5.text")]
    public string? Text5Text { get; set; }

    [JsonPropertyName("Shape5.fill_color")]
    public string? Shape5FillColor { get; set; }

    [JsonPropertyName("Number5.background_color")]
    public string? Number5BackgroundColor { get; set; }
}

internal sealed class CreatomateVideoResponse
{
    [JsonPropertyName("id")]
    public string? VideoId { get; set; }
}

internal sealed class CreatomateVideoQueryResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("url")]
    public string? VideoUrl { get; set; }
}
