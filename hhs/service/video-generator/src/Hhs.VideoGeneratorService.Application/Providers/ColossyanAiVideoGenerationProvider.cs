using System.Net.Http.Headers;
using System.Text;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Colossyan;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.Logging.Abstracts;
using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Providers;

public class ColossyanAiVideoGenerationProvider : IColossyanAiVideoGenerationProvider
{
    private readonly IAppConsoleLogger _logger;

    public ColossyanAiVideoGenerationProvider(IAppConsoleLogger logger)
    {
        _logger = logger;
    }


    public async Task<VideoGenerationSendResponseDto> SendVideoRequestAsync(VideoGenerationSendRequestDto input, bool isProviderSupportPreSignedStorage, ClientColossyanAiSettings videoGenerationProviderSettings, BunnyCdnSelfStorageSettings cdnProviderSettings, string clientZoneName)
    {
        if (input == null || input.VideoContentDatas is { Count: < 1 }) return null;

        var colossyanAiClient = new HttpClient { BaseAddress = new Uri(videoGenerationProviderSettings.ApiBaseUrl) };

        colossyanAiClient.DefaultRequestHeaders.Accept.Clear();
        colossyanAiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        colossyanAiClient.DefaultRequestHeaders.Add("authorizationtoken", videoGenerationProviderSettings.ApiKey);

        string jsonData = JsonConvert.SerializeObject(GenerateVideoRequest(input.VideoContentDatas.Select(x => x.NormalizedContent).JoinAsString(". ")));
        // Studio express -> Add a Voice
        var result = await colossyanAiClient.PostAsync("/video-generation-jobs", new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string resJson = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<ColossyanVideoGenerationResponse>(resJson);
            throw new Exception(errorResponse.Id);
        }

        var returnObject = JsonConvert.DeserializeObject<ColossyanVideoGenerationResponse>(resJson);

        return new VideoGenerationSendResponseDto { ExternalVideoTraceId = returnObject.Id };
    }

    public async Task<VideoGenerationQueryResponseDto> QueryVideoRequestAsync(VideoGenerationQueryRequestDto input, bool isProviderSupportPreSignedStorage, ClientColossyanAiSettings videoGenerationProviderSettings, BunnyCdnSelfStorageSettings cdnProviderSettings, string clientZoneName)
    {
        await Task.Delay(1000);

        return new VideoGenerationQueryResponseDto
        {
            IsVideoReady = true,
            ExternalVideoUrl = "https://www.techsummus.com/test.mpg"
        };
    }

    public Task<VideoGenerationDownloadResponseDto> DownloadAsync(VideoGenerationDownloadRequestDto input)
    {
        throw new NotImplementedException();
    }

    private static ColossyanVideoGenerationRequest GenerateVideoRequest(string input)
    {
        var videoSize = new VideoSize
        {
            Height = "1080",
            Width = "1080"
        };

        var position = new Position
        {
            X = "0",
            Y = "0"
        };

        var track = new Track
        {
            Type = "mp4",
            Actor = "Ryan",
            Position = position,
            Size = videoSize,
            Text = input,
            SpeakerId = "speakerId",
            RemoveBackground = "true"
        };

        var tracks = new List<Track> { track };

        var scn = new Scene
        {
            Name = null,
            Tracks = tracks
        };

        var scenes = new List<Scene> { scn };

        var colossyanVideoGenerationRequest = new ColossyanVideoGenerationRequest
        {
            VideoCreative = new VideoCreative
            {
                Settings = new Settings
                {
                    Name = "Video",
                    VideoSize = new VideoSize
                    {
                        Height = "1080",
                        Width = "1080"
                    }
                },
                Scenes = scenes
            }
        };
        return colossyanVideoGenerationRequest;
    }
}