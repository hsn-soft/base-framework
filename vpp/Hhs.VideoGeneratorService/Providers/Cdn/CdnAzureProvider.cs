using Hhs.Shared.Configuration.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

/// <summary>
/// Azure Blob Storage CDN provider implementation.
/// Stores files in Azure Blob Storage and constructs CDN URLs.
/// </summary>
public sealed class CdnAzureProvider : ICdnProvider
{
    private readonly IHasCdnBaseUrl _cdnSettings;
    private readonly AzureBlobStorageSettings _storageSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnAzureProvider> _logger;

    public CdnAzureProvider(
        IHasCdnBaseUrl cdnSettings,
        AzureBlobStorageSettings storageSettings,
        HttpClient httpClient,
        ILogger<CdnAzureProvider> logger)
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
            var containerName = _storageSettings.BucketOrContainer ?? "default-container";
            var accountName = _storageSettings.Url?.Replace("https://", "").Replace(".blob.core.windows.net", "") ?? "storageaccount";

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file to Azure Blob Storage. Filename: {Filename}, Container: {Container}, Account: {Account}",
                    filename,
                    containerName,
                    accountName);
            }

            // Generate unique filename
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var fileExtension = Path.GetExtension(filename);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var uniqueFilename = $"{fileNameWithoutExtension}_{Guid.NewGuid():N}{fileExtension}";
            var blobName = $"{dateFolder}/{uniqueFilename}";

            // Read stream into bytes
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            // Upload to Azure Blob Storage
            var storageUrl = $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}";
            using (var content = new ByteArrayContent(fileBytes))
            {
                // Add Azure-specific headers
                content.Headers.Add("x-ms-blob-type", "BlockBlob");

                var request = new HttpRequestMessage(HttpMethod.Put, storageUrl)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Azure upload failed with status {response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
                }
            }

            // Create CDN URL
            var cdnUrl = $"{_cdnSettings.BaseUrl.TrimEnd('/')}/{_cdnSettings.ZonePath.Trim('/')}/{_cdnSettings.PathPrefix.Trim('/')}/{blobName}"
                .Replace("//", "/")
                .Replace(":///", "://");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded to Azure successfully. StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    storageUrl,
                    cdnUrl);
            }

            return (storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to Azure Blob Storage", filename);
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
                    "Downloading file from Azure Blob Storage. StorageUrl: {StorageUrl}",
                    storageUrl);
            }

            var response = await _httpClient.GetAsync(storageUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Azure download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from Azure storage URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
