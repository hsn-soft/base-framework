using Hhs.Shared.Configuration.Providers;
using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

/// <summary>
/// S3-compatible CDN storage provider implementation.
/// Supports AWS S3, MinIO, and other S3-compatible services.
/// </summary>
public sealed class CdnS3Provider : ICdnProvider
{
    public string ProviderKey => ProviderKeys.CdnBunnyS3;
    private readonly IHasCdnBaseUrl _cdnSettings;
    private readonly S3StorageSettings _storageSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnS3Provider> _logger;

    public CdnS3Provider(
        IHasCdnBaseUrl cdnSettings,
        S3StorageSettings storageSettings,
        HttpClient httpClient,
        ILogger<CdnS3Provider> logger)
    {
        _cdnSettings = cdnSettings;
        _storageSettings = storageSettings;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(string StorageUrl, string CdnUrl)> UploadAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken)
    {
        try
        {
            string bucketName = _storageSettings.BucketOrContainer ?? "default-bucket";
            string endpoint = _storageSettings.Url ?? "http://localhost:9000";

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file to S3 storage. Filename: {Filename}, Bucket: {Bucket}, Endpoint: {Endpoint}",
                    filename,
                    bucketName,
                    endpoint);
            }

            // Generate unique filename
            string dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            string fileExtension = Path.GetExtension(filename);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            string uniqueFilename = $"{fileNameWithoutExtension}_{Guid.NewGuid():N}{fileExtension}";
            string objectKey = $"{dateFolder}/{uniqueFilename}";

            // Read stream into bytes
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            byte[] fileBytes = memoryStream.ToArray();

            // Upload to S3-compatible endpoint
            string uploadUrl = $"{endpoint.TrimEnd('/')}/{bucketName}/{objectKey}";
            using (var content = new ByteArrayContent(fileBytes))
            {
                var response = await _httpClient.PutAsync(uploadUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"S3 upload failed with status {response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
                }
            }

            // Create storage URL
            string storageUrl = uploadUrl;

            // Create CDN URL
            string cdnUrl = $"{_cdnSettings.BaseUrl.TrimEnd('/')}/{_cdnSettings.ZonePath.Trim('/')}/{_cdnSettings.PathPrefix.Trim('/')}/{objectKey}"
                .Replace("//", "/")
                .Replace(":///", "://");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded to S3 successfully. StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    storageUrl,
                    cdnUrl);
            }

            return (storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to S3 storage", filename);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(
        string storageUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Downloading file from S3 storage. StorageUrl: {StorageUrl}",
                    storageUrl);
            }

            var response = await _httpClient.GetAsync(storageUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"S3 download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from S3 storage URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
