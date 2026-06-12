using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Yepic;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.Logging.Abstracts;
using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Providers;

public class YepicAiVideoGenerationProvider : IYepicAiVideoGenerationProvider
{
    private readonly IAppConsoleLogger _logger;

    public YepicAiVideoGenerationProvider(IAppConsoleLogger logger)
    {
        _logger = logger;
    }

    public async Task<VideoGenerationSendResponseDto> SendVideoRequestAsync(VideoGenerationSendRequestDto input, bool isProviderSupportPreSignedStorage, ClientYepicAiSettings videoGenerationProviderSettings, BunnyCdnSelfStorageSettings cdnProviderSettings, string clientZoneName)
    {
        if (input == null || input.VideoContentDatas is { Count: < 1 }) return null;
        var yepicAiClient = new HttpClient { BaseAddress = new Uri(videoGenerationProviderSettings.ApiBaseUrl) };

        yepicAiClient.DefaultRequestHeaders.Accept.Clear();
        yepicAiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        yepicAiClient.DefaultRequestHeaders.Add("X-Api-Key", videoGenerationProviderSettings.ApiKey);

        var talkingPhoto = new TalkingPhotoRequest
        {
            AvatarId = videoGenerationProviderSettings.AvatarId,
            VoiceId = videoGenerationProviderSettings.VoiceId,
            Script = input.VideoContentDatas.Select(x => x.NormalizedContent).JoinAsString(". "),
            VideoFormat = videoGenerationProviderSettings.VideoFormat.ToString(),
            VideoWidth = videoGenerationProviderSettings.VideoWidth,
            VideoHeight = videoGenerationProviderSettings.VideoHeight,
            VideoTitle = videoGenerationProviderSettings.VideoTitle,
            Visibility = videoGenerationProviderSettings.Visibility
        };

        string jsonData = JsonConvert.SerializeObject(talkingPhoto);
        // Studio express -> Add a Voice
        var result = await yepicAiClient.PostAsync("/v1/talkingphotos", new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string resJson = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<TalkingPhotoResponse>(resJson);
            throw new Exception(errorResponse.Status);
        }

        var returnObject = JsonConvert.DeserializeObject<TalkingPhotoResponse>(resJson);

        return new VideoGenerationSendResponseDto { ExternalVideoTraceId = returnObject.Id };
    }

    public async Task<VideoGenerationQueryResponseDto> QueryVideoRequestAsync(VideoGenerationQueryRequestDto input, bool isProviderSupportPreSignedStorage, ClientYepicAiSettings videoGenerationProviderSettings, BunnyCdnSelfStorageSettings cdnProviderSettings, string clientZoneName)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.ExternalVideoTraceId)) return null;
        var yepicAiClient = new HttpClient { BaseAddress = new Uri(videoGenerationProviderSettings.ApiBaseUrl) };

        yepicAiClient.DefaultRequestHeaders.Accept.Clear();
        yepicAiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        yepicAiClient.DefaultRequestHeaders.Add("X-Api-Key", videoGenerationProviderSettings.ApiKey);

        var result = await yepicAiClient.GetAsync($"/v1/talkingphotos/{input.ExternalVideoTraceId}");
        string resJson = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<TalkingPhotoResponse>(resJson);
            throw new Exception(errorResponse.Status);
        }

        var returnObject = JsonConvert.DeserializeObject<TalkingPhotoResponse>(resJson);
        if (returnObject.DateCreated.HasValue && returnObject.RenderDuration.HasValue)
        {
            return new VideoGenerationQueryResponseDto
            {
                ExternalVideoUrl = returnObject.VideoUrl,
                HasError = false,
                ErrorMessage = null,
                IsVideoReady = true
            };
        }
        else
        {
            return new VideoGenerationQueryResponseDto
            {
                ExternalVideoUrl = returnObject.VideoUrl,
                HasError = false,
                ErrorMessage = null,
                IsVideoReady = false
            };
        }
    }

    public async Task<VideoGenerationDownloadResponseDto> DownloadAsync(VideoGenerationDownloadRequestDto input)
    {
        string fileLocation = null;
        try
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string workingDirectory = Path.GetDirectoryName(path);

            string baseLocation = workingDirectory + "/files/videos";
            if (!Directory.Exists(baseLocation))
            {
                Directory.CreateDirectory(baseLocation);
            }

            _logger.LogDebug($"Starting file download to {baseLocation}");

            fileLocation = await DownloadVideoFileToLocal(input.ExternalVideoUrl, $"{baseLocation}/{Guid.NewGuid().ToString()}{Path.GetExtension(input.ExternalVideoUrl)}");
        }
        catch (Exception e)
        {
            //When download VideoContent returns error
            _logger.LogDebug(e.Message.ToString());
            return new VideoGenerationDownloadResponseDto
            {
                HasError = true
            };
        }

        return new VideoGenerationDownloadResponseDto
        {
            HasError = false,
            LocalVideoPath = fileLocation
        };
    }

    private async Task<string> DownloadVideoFileToLocal(string videoUrl, string fileName)
    {
        using (var httpClient = new HttpClient())
        {
            // Download the video stream from the public URL
            _logger.LogDebug("Downloading");
            using (var memoryStream = new MemoryStream())
            {
                var videoStream = await httpClient.GetStreamAsync(videoUrl);
                await videoStream.CopyToAsync(memoryStream);
                byte[] videoBytes = memoryStream.ToArray();
                using (var fileStream = new FileStream(fileName, FileMode.Create))
                {
                    fileStream.Write(videoBytes);
                }
            }
        }

        return fileName;
    }
}