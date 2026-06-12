using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Did;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Yepic;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using Hhs.VideoGeneratorService.Domain.Enums;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Providers;

public class DidAiVideoGenerationProvider : IDidAiVideoGenerationProvider
{
    #region Static Settings

    private const string ApiBaseUrl = "https://api.d-id.com";
    private const string CropType = "wide"; // çözünürlükle alakalıdır
    private const string UserData = "CustomerUserData"; // api response customize yapabilme flag i
    private const string ScriptSsml = "false"; // duraksama ayarları için gerekli
    private const string ScriptSubtitle = "false"; // altyazı olsun mu
    private const string ScriptType = "text";
    private const string ResultFormat = nameof(VideoFormatTypes.mp4);
    private const int OutputResolution = 720;

    #endregion

    private readonly IAppConsoleLogger _logger;
    private readonly DidAiSettings _didAiSettings;
    private readonly BunnyCdnS3StorageSettings _bunnyCdnS3StorageSettings;

    public DidAiVideoGenerationProvider(IAppConsoleLogger logger,
        IOptions<DidAiSettings> didAiSettings,
        IOptions<BunnyCdnS3StorageSettings> bunnyCdnS3StorageSettings)
    {
        _logger = logger;
        _didAiSettings = didAiSettings?.Value ?? throw new ArgumentNullException(nameof(didAiSettings));
        _bunnyCdnS3StorageSettings = bunnyCdnS3StorageSettings?.Value ?? throw new ArgumentNullException(nameof(bunnyCdnS3StorageSettings));
    }

    public async Task<VideoGenerationSendResponseDto> SendVideoRequestAsync(VideoGenerationSendRequestDto input, bool isProviderSupportPreSignedStorage, ClientDidAiSettings videoGenerationProviderSettings, BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        if (input == null || input.VideoContentDatas is { Count: < 1 }) return null;
        var didAiClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };

        didAiClient.DefaultRequestHeaders.Accept.Clear();
        didAiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        didAiClient.DefaultRequestHeaders.Add("authorization", $"Basic {_didAiSettings.ApiKey}");

        var didClip = await GenerateVideoRequest(input, isProviderSupportPreSignedStorage, videoGenerationProviderSettings, cdnProviderSettings, clientZoneName);
        string jsonData = JsonConvert.SerializeObject(didClip, Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        var result =
            await didAiClient.PostAsync("/clips", new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string resJson = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<DidClipErrorResponse>(resJson);
            throw new Exception(errorResponse.ErrorMessage);
        }

        var returnObject = JsonConvert.DeserializeObject<TalkingPhotoResponse>(resJson);

        return new VideoGenerationSendResponseDto { ExternalVideoTraceId = returnObject.Id };
    }

    public async Task<VideoGenerationQueryResponseDto> QueryVideoRequestAsync(VideoGenerationQueryRequestDto input, bool isProviderSupportPreSignedStorage, ClientDidAiSettings videoGenerationProviderSettings, BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.ExternalVideoTraceId)) return null;
        var didAiClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };

        didAiClient.DefaultRequestHeaders.Accept.Clear();
        didAiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        didAiClient.DefaultRequestHeaders.Add("authorization", $"Basic {_didAiSettings.ApiKey}");

        var result = await didAiClient.GetAsync($"/clips/{input.ExternalVideoTraceId}");
        string resJson = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<DidClipResponse>(resJson);
            throw new Exception(errorResponse.Status);
        }

        var returnObject = JsonConvert.DeserializeObject<DidClipResponse>(resJson);
        if (!string.IsNullOrWhiteSpace(returnObject.ResultUrl) && returnObject.ResultUrl.Length > 50)
        {
            _logger.LogInformation("Video Ready : " + returnObject.ResultUrl);
            if (isProviderSupportPreSignedStorage)
            {
                if (returnObject.ResultUrl.Contains("backblaze"))
                {
                    string fileName = Path.GetFileName(returnObject.ResultUrl);
                    if (fileName.Contains('?'))
                    {
                        fileName = fileName.Split("?")[0];
                    }

                    bool fileIsReady = await CheckIfFileIsReadyOnStorage(fileName, cdnProviderSettings, clientZoneName);
                    if (fileIsReady)
                    {
                        return new VideoGenerationQueryResponseDto
                        {
                            ExternalVideoUrl = returnObject.ResultUrl,
                            HasError = false,
                            ErrorMessage = null,
                            IsVideoReady = true,
                            StorageVideoUrl = $"https://{clientZoneName}{cdnProviderSettings.PullZoneUrl}{fileName}"
                        };
                    }
                }

                _logger.LogError("COULD NOT GENERATE PRES3 URL - DOWNLOAD/UPLOAD MODE ON");
                return new VideoGenerationQueryResponseDto
                {
                    ExternalVideoUrl = returnObject.ResultUrl,
                    HasError = false,
                    ErrorMessage = null,
                    IsVideoReady = false,
                    StorageVideoUrl = null
                };
            }

            return new VideoGenerationQueryResponseDto
            {
                ExternalVideoUrl = returnObject.ResultUrl,
                HasError = false,
                ErrorMessage = null,
                IsVideoReady = true,
                StorageVideoUrl = null
            };
        }

        return new VideoGenerationQueryResponseDto
        {
            ExternalVideoUrl = returnObject.ResultUrl,
            HasError = false,
            ErrorMessage = null,
            IsVideoReady = false,
            StorageVideoUrl = null
        };
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
            // .mp4?AWSAccessKeyId=AKIA5CUMPJBIJ7CPKJNP&Expires=1728328801&Signature=DV1gsvYo7T6lLLz%2FdNtJnMvfCCE%3D
            string fileExtension = Path.GetExtension(input.ExternalVideoUrl);
            if (fileExtension.Contains('?'))
            {
                fileExtension = fileExtension.Split("?")[0];
            }

            fileLocation = await DownloadVideoFileToLocal(input.ExternalVideoUrl,
                $"{baseLocation}/{Guid.NewGuid().ToString()}{fileExtension}");
        }
        catch (Exception e)
        {
            //When download VideoContent returns error
            _logger.LogDebug(e.Message);
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
        using var httpClient = new HttpClient();
        // Download the video stream from the public URL
        _logger.LogDebug("Downloading");
        using var memoryStream = new MemoryStream();
        var videoStream = await httpClient.GetStreamAsync(videoUrl);
        await videoStream.CopyToAsync(memoryStream);
        byte[] videoBytes = memoryStream.ToArray();
        await using var fileStream = new FileStream(fileName, FileMode.Create);
        fileStream.Write(videoBytes);

        return fileName;
    }

    private async Task<DidClipRequest> GenerateVideoRequest(VideoGenerationSendRequestDto input, bool isEnabledPreSignedUrlForPublisher, ClientDidAiSettings clientDidAiSettings, BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        string name = $"{input.VideoRequestId.ToString()}.{ResultFormat}";

        string resultUrl = null;
        if (isEnabledPreSignedUrlForPublisher)
        {
            try
            {
                if (input.VideoRequestId == Guid.Empty) throw new Exception("Invalid video request id");
                resultUrl = await CreatePreS3Url(name, cdnProviderSettings, clientZoneName);
            }
            catch (Exception e)
            {
                _logger.LogError("Error On Creating PreS3SignedURL - Message:{1}", e.Message);
            }
        }


        var provider = new Provider
        {
            Type = clientDidAiSettings.ProviderType,
            VoiceId = clientDidAiSettings.ProviderVoiceId,
            ModelId = clientDidAiSettings.ProviderModelId
        };

        return new DidClipRequest
        {
            Name = name,
            PresenterConfig = new PresenterConfig
            {
                Crop = new Crop
                {
                    Type = CropType
                }
            },
            //TODO: Correct AudioFileList for DidAi
            Script = new Script
            {
                //Input = string.IsNullOrWhiteSpace(input.AudioFileName) ? input.VideoContentDatas.Select(x => x.NormalizedContent).JoinAsString(". ") : null,
                Ssml = ScriptSsml,
                Subtitles = ScriptSubtitle
                //Type = string.IsNullOrWhiteSpace(input.AudioFileName) ? "text" : "audio", //settings.ScriptType,
                //Provider = string.IsNullOrWhiteSpace(input.AudioFileName) ? provider : null,
                //AudioUrl = input.AudioFileName
            },
            Background = new Background
            {
                SourceUrl = clientDidAiSettings.BackgroundSourceUrl
            },
            DriverId = clientDidAiSettings.DriverId,
            PresenterId = clientDidAiSettings.PresenterId,
            UserData = UserData,
            Config = new Config
            {
                Logo = new Logo
                {
                    Position = new[] { clientDidAiSettings.LogoPosition.Y, clientDidAiSettings.LogoPosition.X },
                    Url = clientDidAiSettings.LogoUrl
                },
                ResultFormat = ResultFormat,
                OutputResolution = OutputResolution
            },
            ResultUrl = resultUrl
        };
    }

    private IAmazonS3 CreateS3Client(string accessKey, string secretKey, string credentialProfile)
    {
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var uri = new UriBuilder(codeBase);
        string path = Uri.UnescapeDataString(uri.Path);
        string workingDirectory = Path.GetDirectoryName(path);

        var chain = new CredentialProfileStoreChain(workingDirectory + "/credentials");
        if (!chain.TryGetAWSCredentials(credentialProfile, out var awsCredentials))
        {
            _logger.LogWarning("Could not find credentials profile. Creating...");
            var options = new CredentialProfileOptions
            {
                AccessKey = accessKey,
                SecretKey = secretKey
            };

            var sharedFile = new SharedCredentialsFile(workingDirectory + "/credentials");
            sharedFile.RegisterProfile(new CredentialProfile(credentialProfile, options));
        }

        if (chain.TryGetAWSCredentials(credentialProfile, out awsCredentials))
        {
            return new AmazonS3Client(awsCredentials, new AmazonS3Config { ServiceURL = _bunnyCdnS3StorageSettings.ApiBaseUrl });
        }

        throw new Exception("S3 Credentials error");
    }

    private async Task<string> CreatePreS3Url(string key, BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        var s3Client = CreateS3Client(cdnProviderSettings.ApiKey, cdnProviderSettings.ApiSecret, clientZoneName);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = clientZoneName,
            Key = key, //Filename
            Verb = HttpVerb.PUT,
            Expires = DateTime.Now.AddHours(1)
        };
        return await s3Client.GetPreSignedURLAsync(request);
    }

    private async Task<bool> CheckIfFileIsReadyOnStorage(string key, BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        try
        {
            var s3Client = CreateS3Client(cdnProviderSettings.ApiKey, cdnProviderSettings.ApiSecret, clientZoneName);
            var request = new GetObjectMetadataRequest
            {
                BucketName = clientZoneName,
                Key = key
            };
            var response = await s3Client.GetObjectMetadataAsync(request);
            return response.HttpStatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.NotFound) && response.ContentLength > 10000;
        }
        catch (Exception e)
        {
            _logger.LogError("Error On Checking File Existence - Message:{1}", e.Message);
            return false;
        }
    }
}