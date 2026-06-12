using System.Reflection;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Hhs.VideoGeneratorService.Application.Contracts.Providers;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage;
using Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.Storage.Bunny;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hhs.VideoGeneratorService.Application.Providers;

public class BunnyCdnBlackBlazeVideoStorageProvider : IBunnyCdnS3VideoStorageProvider
{
    private readonly IAppConsoleLogger _logger;
    private readonly IHostEnvironment _env;
    private readonly BunnyCdnS3StorageSettings _bunnyCdnS3StorageSettings;

    public BunnyCdnBlackBlazeVideoStorageProvider(IAppConsoleLogger logger, IOptions<BunnyCdnS3StorageSettings> bunnyCdnS3StorageSettings, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
        _bunnyCdnS3StorageSettings = bunnyCdnS3StorageSettings?.Value ?? throw new ArgumentNullException(nameof(bunnyCdnS3StorageSettings));
    }

    public async Task<FileStorageUploadResponseDto> UploadAsync(FileStorageUploadRequestDto input, BunnyCdnS3StorageSettings videoStorageProviderSettings, string clientZoneName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.LocalFilePath)) throw new Exception("Unknown local file path");

            _logger.LogDebug("Starting Upload to Storage from Local Folder");
            await UploadVideoToStorage(input.LocalFilePath, clientZoneName);
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

    private async Task UploadVideoToStorage(string inputVideoFileLocation, string clientZoneName)
    {
        var s3Client = CreateS3Client(
            _bunnyCdnS3StorageSettings.ApiKey,
            _bunnyCdnS3StorageSettings.ApiSecret,
            clientZoneName);

        var request = new PutObjectRequest
        {
            BucketName = clientZoneName,
            Key = Path.GetFileName(inputVideoFileLocation),
            FilePath = inputVideoFileLocation,
            ContentType = "application/octet-stream"
        };
        var response = await s3Client.PutObjectAsync(request);
    }

    private IAmazonS3 CreateS3Client(string accessKey, string secretKey, string credentialProfile)
    {
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var uri = new UriBuilder(codeBase);
        string path = Uri.UnescapeDataString(uri.Path);
        string workingDirectory = Path.GetDirectoryName(path);

        string baseLocation = _env.EnvironmentName is "production" or "stage" ? "/tmp/credentials" : workingDirectory + "/credentials";

        var chain = new CredentialProfileStoreChain(baseLocation);
        if (!chain.TryGetAWSCredentials(credentialProfile, out var awsCredentials))
        {
            _logger.LogError("Could not find credentials profile. Creating...");
            var options = new CredentialProfileOptions
            {
                AccessKey = accessKey,
                SecretKey = secretKey
            };

            var sharedFile = new SharedCredentialsFile(baseLocation);
            _logger.LogWarning("Registering...");
            sharedFile.RegisterProfile(new CredentialProfile(credentialProfile, options));
        }

        if (chain.TryGetAWSCredentials(credentialProfile, out awsCredentials))
        {
            _logger.LogWarning("Has been found credentials profile.");
            return new AmazonS3Client(awsCredentials, new AmazonS3Config
            {
                ServiceURL = _bunnyCdnS3StorageSettings.ApiBaseUrl,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
        }

        throw new Exception("S3 Credentials error");
    }
}