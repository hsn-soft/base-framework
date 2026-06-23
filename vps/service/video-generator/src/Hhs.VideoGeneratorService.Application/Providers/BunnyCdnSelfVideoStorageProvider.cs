using System.Net.Http.Headers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.Application.Providers;

public class BunnyCdnSelfVideoStorageProvider : IBunnyCdnSelfVideoStorageProvider
{
    private readonly IAppConsoleLogger _logger;

    public BunnyCdnSelfVideoStorageProvider(IAppConsoleLogger logger)
    {
        _logger = logger;
    }

    public async Task<FileStorageUploadResponseDto> UploadAsync(FileStorageUploadRequestDto input, BunnyCdnSelfStorageSettings videoStorageProviderSettings, string clientZoneName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.LocalFilePath)) throw new Exception("Unknown local file path");

            _logger.LogDebug("Starting Upload to Storage from Local Folder");
            await UploadVideoToStorage(input.LocalFilePath, videoStorageProviderSettings,clientZoneName);
        }
        catch (Exception e)
        {
            return new FileStorageUploadResponseDto
            {
                HasError = true,
                ErrorMessage = e.Message
            };
        }

        return new FileStorageUploadResponseDto
        {
            HasError = false,
            FileTraceId = null,
            FileStorageUrl = $"https://{clientZoneName}{videoStorageProviderSettings.PullZoneUrl}{Path.GetFileName(input.LocalFilePath)}"
        };
    }

    private async Task UploadVideoToStorage(string inputVideoFileLocation, BunnyCdnSelfStorageSettings videoStorageProviderSettings, string clientZoneName)
    {
        _logger.LogDebug("Reading from file");
        await using var videoStream = File.OpenRead(inputVideoFileLocation);
        _logger.LogDebug("Uploading");
        await UploadStreamToCloudStorage(videoStream, $"{clientZoneName}{videoStorageProviderSettings.Path}{Path.GetFileName(inputVideoFileLocation)}", videoStorageProviderSettings);
    }

    private async Task UploadStreamToCloudStorage(Stream videoStream, string getFileName, BunnyCdnSelfStorageSettings videoStorageProviderSettings)
    {
        var bunnyCdnClient = new HttpClient { BaseAddress = new Uri(videoStorageProviderSettings.ApiBaseUrl) };
        bunnyCdnClient.DefaultRequestHeaders.Accept.Clear();
        bunnyCdnClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        bunnyCdnClient.DefaultRequestHeaders.Add("AccessKey", videoStorageProviderSettings.ApiKey);

        byte[] videoBytes;
        using (var memoryStream = new MemoryStream())
        {
            await videoStream.CopyToAsync(memoryStream);
            videoBytes = memoryStream.ToArray();
        }

        var content = new ByteArrayContent(videoBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var result = await bunnyCdnClient.PutAsync($"/{getFileName}", content);
        if (!result.IsSuccessStatusCode)
        {
            throw new Exception(result.StatusCode.ToString());
        }

        _logger.LogDebug("Update is successful");
    }
}