using Hhs.Shared.Helper.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;
using Microsoft.Extensions.Logging;

namespace Hhs.VideoGeneratorService.Application.Providers.Cdn;

public sealed class CdnBunnyS3Provider : ICdnProvider
{
    public string ProviderKey => ProviderKeys.CdnBunnyS3;
    private readonly CdnBunnyS3Settings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnBunnyS3Provider> _logger;

    public CdnBunnyS3Provider(
        CdnBunnyS3Settings settings,
        HttpClient httpClient,
        ILogger<CdnBunnyS3Provider> logger)
    {
        _settings = settings;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CdnUploadResult> UploadAsync(Stream fileStream, string filename)
    {
        try
        {
            string bucketName = _settings.StorageType ?? "default-bucket";
            string endpoint = _settings.StorageEndpointUrl ?? "http://localhost:9000";

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file to S3 storage. Filename: {Filename}, Bucket: {Bucket}, Endpoint: {Endpoint}",
                    filename,
                    bucketName,
                    endpoint);
            }

            string dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            string fileExtension = Path.GetExtension(filename);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            string uniqueFilename = $"{fileNameWithoutExtension}_{Guid.NewGuid():N}{fileExtension}";
            string objectKey = $"{dateFolder}/{uniqueFilename}";

            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();

            string uploadUrl = $"{endpoint.TrimEnd('/')}/{bucketName}/{objectKey}";
            using (var content = new ByteArrayContent(fileBytes))
            {
                var response = await _httpClient.PutAsync(uploadUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"S3 upload failed with status {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                }
            }

            string storageUrl = uploadUrl;
            string cdnUrl = $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.ZonePath.Trim('/')}/{_settings.PathPrefix.Trim('/')}/{objectKey}"
                .Replace("//", "/")
                .Replace(":///", "://");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded to S3 successfully. StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    storageUrl,
                    cdnUrl);
            }

            return new CdnUploadResult(storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to S3 storage", filename);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string storageUrl)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Downloading file from S3 storage. StorageUrl: {StorageUrl}",
                    storageUrl);
            }

            var response = await _httpClient.GetAsync(storageUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"S3 download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from S3 storage URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
