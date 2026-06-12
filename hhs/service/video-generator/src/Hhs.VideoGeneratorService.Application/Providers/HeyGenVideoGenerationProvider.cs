using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Hhs.Shared.Helper.Enums;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.HeyGen;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hhs.VideoGeneratorService.Application.Providers;

public class HeyGenVideoGenerationProvider : IHeyGenVideoGenerationProvider
{
    private const string ApiBaseUrl = "https://api.heygen.com";

    private readonly IAppConsoleLogger _logger;
    private readonly IHostEnvironment _env;
    private readonly HeyGenSettings _heyGenSettings;
    private readonly BunnyCdnS3StorageSettings _bunnyCdnS3StorageSettings;

    public HeyGenVideoGenerationProvider(IAppConsoleLogger logger,
        IOptions<HeyGenSettings> heyGenSettings,
        IOptions<BunnyCdnS3StorageSettings> bunnyCdnS3StorageSettings, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
        _heyGenSettings = heyGenSettings?.Value ?? throw new ArgumentNullException(nameof(heyGenSettings));
        _bunnyCdnS3StorageSettings = bunnyCdnS3StorageSettings?.Value ?? throw new ArgumentNullException(nameof(bunnyCdnS3StorageSettings));
    }

    public async Task<VideoGenerationSendResponseDto> SendVideoRequestAsync(VideoGenerationSendRequestDto input, bool isProviderSupportPreSignedStorage, ClientHeyGenSettings videoGenerationProviderSettings,
        BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        if (input == null || input.VideoContentDatas is { Count: < 1 }) return null;
        var heyGenClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };

        string templateId = input.RefContentType == ReferenceContentTypes.ANALYSIS_CONTENT
            ? videoGenerationProviderSettings.AnalysisVideoTemplateId
            : videoGenerationProviderSettings.DirectVideoTemplateId;

        string requestUri = "/v2/template/" + templateId + "/generate";

        heyGenClient.DefaultRequestHeaders.Accept.Clear();
        heyGenClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        heyGenClient.DefaultRequestHeaders.Add("X-Api-Key", _heyGenSettings.ApiKey);

        var heygenClip = await GenerateVideoRequest(input, videoGenerationProviderSettings);

        string jsonData = JsonConvert.SerializeObject(heygenClip, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        var result = await heyGenClient.PostAsync(requestUri, new StringContent(jsonData, Encoding.UTF8, "application/json"));
        string resJson = await result.Content.ReadAsStringAsync();
        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<HeyGenVideoErrorResponse>(resJson);
            throw new Exception(errorResponse.Error.Message);
        }

        var returnObject = JsonConvert.DeserializeObject<HeyGenVideoResponse>(resJson);
        return new VideoGenerationSendResponseDto { ExternalVideoTraceId = returnObject.Data.VideoId };
    }

    public async Task<VideoGenerationQueryResponseDto> QueryVideoRequestAsync(VideoGenerationQueryRequestDto input, bool isProviderSupportPreSignedStorage, ClientHeyGenSettings videoGenerationProviderSettings,
        BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.ExternalVideoTraceId)) return null;
        var heyGenClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };

        heyGenClient.DefaultRequestHeaders.Accept.Clear();
        heyGenClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        heyGenClient.DefaultRequestHeaders.Add("X-Api-Key", _heyGenSettings.ApiKey);

        var result = await heyGenClient.GetAsync($"/v1/video_status.get?video_id={input.ExternalVideoTraceId}");
        string resJson = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<HeyGenVideoErrorResponse>(resJson);
            throw new Exception(errorResponse.Error.Message);
        }

        var returnObject = JsonConvert.DeserializeObject<HeyGenVideoQueryResponse>(resJson);

        if (!string.IsNullOrWhiteSpace(returnObject.Data.Status))
        {
            if (returnObject.Data.Status.Equals("completed"))
            {
                _logger.LogInformation("Video Ready : " + returnObject.Data.VideoUrl);
                if (isProviderSupportPreSignedStorage)
                {
                    if (returnObject.Data.VideoUrl.Contains("backblaze"))
                    {
                        string fileName = Path.GetFileName(returnObject.Data.VideoUrl);
                        if (fileName.Contains('?'))
                        {
                            fileName = fileName.Split("?")[0];
                        }

                        bool fileIsReady = await CheckIfFileIsReadyOnStorage(fileName, cdnProviderSettings, clientZoneName);
                        if (fileIsReady)
                        {
                            return new VideoGenerationQueryResponseDto
                            {
                                ExternalVideoUrl = returnObject.Data.VideoUrl,
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
                        ExternalVideoUrl = returnObject.Data.VideoUrl,
                        HasError = false,
                        ErrorMessage = null,
                        IsVideoReady = false,
                        StorageVideoUrl = null
                    };
                }

                return new VideoGenerationQueryResponseDto
                {
                    ExternalVideoUrl = returnObject.Data.VideoUrl,
                    HasError = false,
                    ErrorMessage = null,
                    IsVideoReady = true,
                    StorageVideoUrl = null
                };
            }

            if (returnObject.Data.Status.Equals("failed"))
            {
                return new VideoGenerationQueryResponseDto
                {
                    ExternalVideoUrl = returnObject.Data.VideoUrl,
                    HasError = true,
                    ErrorMessage = returnObject.Data.Error,
                    IsVideoReady = false,
                    StorageVideoUrl = null
                };
            }
        }

        return new VideoGenerationQueryResponseDto
        {
            ExternalVideoUrl = returnObject.Data.VideoUrl,
            HasError = false,
            ErrorMessage = null,
            IsVideoReady = false,
            StorageVideoUrl = null
        };
    }

    public async Task<VideoGenerationDownloadResponseDto> DownloadAsync(VideoGenerationDownloadRequestDto input)
    {
        string fileLocation;
        try
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string workingDirectory = Path.GetDirectoryName(path);

            string baseLocation = _env.EnvironmentName is "production" or "stage" ? "/tmp/videos" : workingDirectory + "/files/videos";
            if (!Directory.Exists(baseLocation))
            {
                Directory.CreateDirectory(baseLocation);
            }

            _logger.LogDebug($"Starting file download to {baseLocation}");
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
            _logger.LogError("DOWNLOAD_UNHANDLED_EXCEPTION => " + e.Message);
            return new VideoGenerationDownloadResponseDto { HasError = true, ErrorMessage = e.Message };
        }

        return new VideoGenerationDownloadResponseDto { HasError = false, LocalVideoPath = fileLocation };
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

    private async Task<HeyGenVideoRequest> GenerateVideoRequest(VideoGenerationSendRequestDto input, ClientHeyGenSettings clientHeyGenSettings)
    {
        string templateId = input.RefContentType == ReferenceContentTypes.ANALYSIS_CONTENT
            ? clientHeyGenSettings.AnalysisVideoTemplateId
            : clientHeyGenSettings.DirectVideoTemplateId;

        var apiTemplateFields = await GetTemplateFieldsById(templateId);
        if (apiTemplateFields is { Count: < 1 })
        {
            throw new Exception("TEMPLATE_FILEDS_ARE_NOT_FOUND");
        }

        dynamic genericModel = GetDynamicRequestObject(clientHeyGenSettings, apiTemplateFields, input);
        return new HeyGenVideoRequest
        {
            Variables = JsonConvert.DeserializeObject<dynamic>(genericModel), TemplateId = templateId, Dimension = new Dimension { Height = clientHeyGenSettings.VideoHeight, Width = clientHeyGenSettings.VideoWidth }
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
            var options = new CredentialProfileOptions { AccessKey = accessKey, SecretKey = secretKey };

            var sharedFile = new SharedCredentialsFile(workingDirectory + "/credentials");
            sharedFile.RegisterProfile(new CredentialProfile(credentialProfile, options));
        }

        if (chain.TryGetAWSCredentials(credentialProfile, out awsCredentials))
        {
            return new AmazonS3Client(awsCredentials, new AmazonS3Config { ServiceURL = _bunnyCdnS3StorageSettings.ApiBaseUrl });
        }

        throw new Exception("S3 Credentials error");
    }

    private async Task<bool> CheckIfFileIsReadyOnStorage(string key, BunnyCdnS3StorageSettings cdnProviderSettings, string clientZoneName)
    {
        try
        {
            var s3Client = CreateS3Client(cdnProviderSettings.ApiKey, cdnProviderSettings.ApiSecret, clientZoneName);
            var request = new GetObjectMetadataRequest { BucketName = clientZoneName, Key = key };
            var response = await s3Client.GetObjectMetadataAsync(request);
            return response.HttpStatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.NotFound) && response.ContentLength > 10000;
        }
        catch (Exception e)
        {
            _logger.LogError("Error On Checking File Existence - Message:{1}", e.Message);
            return false;
        }
    }

    private async Task<List<KeyValuePair<string, string>>> GetTemplateFieldsById(string templateId)
    {
        var fields = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(templateId)) return fields;

        var heyGenClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
        string requestUri = "/v2/template/" + templateId;

        heyGenClient.DefaultRequestHeaders.Accept.Clear();
        heyGenClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        heyGenClient.DefaultRequestHeaders.Add("X-Api-Key", _heyGenSettings.ApiKey);

        var result = await heyGenClient.GetAsync(requestUri);
        string resJson = await result.Content.ReadAsStringAsync();
        if (!result.IsSuccessStatusCode)
        {
            var errorResponse = JsonConvert.DeserializeObject<HeyGenVideoErrorResponse>(resJson);
            throw new Exception(errorResponse.Error.Message);
        }

        var resultJsonObject = JsonConvert.DeserializeObject<JObject>(resJson);
        if (resultJsonObject == null) return fields;

        resultJsonObject.TryGetValue("data", out var dataObject);
        if (dataObject == null) return fields;

        dataObject.As<JObject>().TryGetValue("variables", out var variablesObject);
        if (variablesObject == null) return fields;

        foreach (var property in variablesObject.As<JObject>().Properties())
        {
            property.Value.As<JObject>().TryGetValue("name", out var propertyNameObject);
            string fieldName = propertyNameObject.As<JValue>().Value?.ToString();

            property.Value.As<JObject>().TryGetValue("type", out var propertyTypeObject);
            string fieldType = propertyTypeObject.As<JValue>().Value?.ToString();

            fields.Add(new KeyValuePair<string, string>(fieldName ?? string.Empty, fieldType ?? string.Empty));
        }

        return fields;
    }

    private static dynamic GetDynamicRequestObject(ClientHeyGenSettings clientHeyGenSettings, List<KeyValuePair<string, string>> apiTemplateDataFieldList, VideoGenerationSendRequestDto input)
    {
        if (apiTemplateDataFieldList is { Count: < 1 } || input.VideoContentDatas is { Count: < 1 })
        {
            throw new Exception("FIELD_COUNT_OR_INPUT_COUNT_ARE_NOT_EMPTY");
        }

        string variablesJsonData = "";

        var publisherLogoField = apiTemplateDataFieldList.FirstOrDefault(x => x.Key.StartsWith("publisher_logo"));
        if (!string.IsNullOrWhiteSpace(publisherLogoField.Key) && !string.IsNullOrWhiteSpace(publisherLogoField.Value))
        {
            // add publisher logo
            var publisherLogoObject = new InnerVariable { Name = "publisher_logo", Type = "image", Properties = new Properties { Url = clientHeyGenSettings.LogoUrl, Fit = "cover" } };
            variablesJsonData += CreateGenericModelContent(publisherLogoObject) + ",";
        }

        if (input.RefContentType == ReferenceContentTypes.APP_REQUEST_CONTENT)
        {
            InnerVariable voiceObject;
            var bgObject = new InnerVariable { Name = "bg_image", Type = "image", Properties = new Properties { Url = clientHeyGenSettings.BackgroundImageUrl, Fit = "cover" } };


            if (input.AudioFileNames is { Count: > 0 })
            {
                voiceObject = new InnerVariable
                {
                    Name = "audio_0",
                    Type = "audio",
                    Properties = new Properties
                    {
                        Url = input.AudioFileNames[0]
                    }
                };
            }
            else
            {
                voiceObject = new InnerVariable
                {
                    Name = "script_en_0",
                    Type = "text",
                    Properties = new Properties
                    {
                        Content = input.VideoContentDatas.Select(x => x.NormalizedContent).JoinAsString(". ")
                    }
                };
            }

            variablesJsonData += CreateGenericModelContent(voiceObject) + "," + CreateGenericModelContent(bgObject);
        }
        else // ANALYSIS CONTENT TEMPLATE MODEL GENERATION
        {
            var scriptFields = apiTemplateDataFieldList
                .Where(x => x.Key.StartsWith("script_en_"))
                .OrderBy(x => x.Key)
                .ToList();
            if (scriptFields is { Count: > 0 })
            {
                // index control
                if (input.VideoContentDatas.Count != scriptFields.Count)
                {
                    throw new Exception("FIELD_COUNT_AND_INPUT_COUNT_NOT_THE_SAME");
                }

                // add script fields
                for (int i = 0; i < scriptFields.Count; i++)
                {
                    var scriptObject = new InnerVariable { Name = scriptFields[i].Key, Type = scriptFields[i].Value, Properties = new Properties { Content = input.VideoContentDatas[i].NormalizedContent } };
                    variablesJsonData += CreateGenericModelContent(scriptObject) + ",";
                }
            }

            var textFields = apiTemplateDataFieldList
                .Where(x => x.Key.StartsWith("text_"))
                .OrderBy(x => x.Key)
                .ToList();
            if (textFields is { Count: > 0 })
            {
                // index control
                if (input.VideoContentDatas.Count != textFields.Count)
                {
                    throw new Exception("FIELD_COUNT_AND_INPUT_COUNT_NOT_THE_SAME");
                }

                // add text fields
                for (int i = 0; i < textFields.Count; i++)
                {
                    var textObject = new InnerVariable { Name = textFields[i].Key, Type = textFields[i].Value, Properties = new Properties { Content = input.VideoContentDatas[i].TitleText } };
                    variablesJsonData += CreateGenericModelContent(textObject) + ",";
                }
            }

            var bgImageFields = apiTemplateDataFieldList
                .Where(x => x.Key.StartsWith("bg_image_"))
                .OrderBy(x => x.Key)
                .ToList();
            if (bgImageFields is { Count: > 0 })
            {
                // index control
                if (input.VideoContentDatas.Count != bgImageFields.Count)
                {
                    throw new Exception("FIELD_COUNT_AND_INPUT_COUNT_NOT_THE_SAME");
                }

                // add background image fields
                for (int i = 0; i < bgImageFields.Count; i++)
                {
                    var bgObject = new InnerVariable
                    {
                        Name = bgImageFields[i].Key,
                        Type = bgImageFields[i].Value,
                        Properties = new Properties
                        {
                            Url = string.IsNullOrWhiteSpace(input.VideoContentDatas[i].ImageUrl)
                                ? clientHeyGenSettings.BackgroundImageUrl
                                : input.VideoContentDatas[i].ImageUrl,
                            Fit = "cover"
                        }
                    };
                    variablesJsonData += CreateGenericModelContent(bgObject) + ",";
                }
            }

            var audioFields = apiTemplateDataFieldList
                .Where(x => x.Key.StartsWith("audio_"))
                .OrderBy(x => x.Key)
                .ToList();
            if (audioFields is { Count: > 0 })
            {
                // index control
                if (input.VideoContentDatas.Count != audioFields.Count)
                {
                    throw new Exception("FIELD_COUNT_AND_INPUT_COUNT_NOT_THE_SAME");
                }

                // add audio fields
                for (var i = 0; i < audioFields.Count; i++)
                {
                    var audioObject = new InnerVariable
                    {
                        Name = audioFields[i].Key,
                        Type = audioFields[i].Value,
                        Properties = new Properties
                        {
                            Url = input.AudioFileNames[i]
                        }
                    };
                    variablesJsonData += CreateGenericModelContent(audioObject) + ",";
                }
            }

            if (!string.IsNullOrWhiteSpace(variablesJsonData))
            {
                // clear last comma
                variablesJsonData = variablesJsonData.Substring(0, variablesJsonData.Length - 1);
            }
        }

        return "{" + variablesJsonData + "}";
    }

    private static string CreateGenericModelContent(InnerVariable input)
        => $"\"{input.Name}\" :{JsonConvert.SerializeObject(input, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })}";
}