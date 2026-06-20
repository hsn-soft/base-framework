using Microsoft.Extensions.Logging;
using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers;
using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Providers.Storage;

/// <summary>
/// Cloudflare R2 CDN storage provider implementation.
/// Stores files in Cloudflare R2 object storage and constructs CDN URLs.
/// </summary>
public sealed class CloudflareR2StorageProvider : ICdnStorageProvider
{
    private readonly IHasCdnBaseUrl _cdnSettings;
    private readonly CloudflareR2StorageSettings _storageSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudflareR2StorageProvider> _logger;

    public CloudflareR2StorageProvider(
        IHasCdnBaseUrl cdnSettings,
        CloudflareR2StorageSettings storageSettings,
        HttpClient httpClient,
        ILogger<CloudflareR2StorageProvider> logger)
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
            var bucketName = _storageSettings.BucketName ?? "default-bucket";
            var endpoint = _storageSettings.Endpoint ?? $"https://{_storageSettings.AccountId}.r2.cloudflarestorage.com";

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file to Cloudflare R2. Filename: {Filename}, Bucket: {Bucket}, Endpoint: {Endpoint}",
                    filename,
                    bucketName,
                    endpoint);
            }

            // Generate unique filename
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var fileExtension = Path.GetExtension(filename);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var uniqueFilename = $"{fileNameWithoutExtension}_{Guid.NewGuid():N}{fileExtension}";
            var objectKey = $"{dateFolder}/{uniqueFilename}";

            // Read stream into bytes
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            // Upload to Cloudflare R2
            var uploadUrl = $"{endpoint.TrimEnd('/')}/{bucketName}/{objectKey}";
            using (var content = new ByteArrayContent(fileBytes))
            {
                var response = await _httpClient.PutAsync(uploadUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Cloudflare R2 upload failed with status {response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
                }
            }

            // Create storage URL
            var storageUrl = uploadUrl;

            // Create CDN URL
            var cdnUrl = $"{_cdnSettings.BaseUrl.TrimEnd('/')}/{_cdnSettings.ZonePath.Trim('/')}/{_cdnSettings.PathPrefix.Trim('/')}/{objectKey}"
                .Replace("//", "/")
                .Replace(":///", "://");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded to Cloudflare R2 successfully. StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    storageUrl,
                    cdnUrl);
            }

            return (storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to Cloudflare R2", filename);
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
                    "Downloading file from Cloudflare R2. StorageUrl: {StorageUrl}",
                    storageUrl);
            }

            var response = await _httpClient.GetAsync(storageUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Cloudflare R2 download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from Cloudflare R2 storage URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
