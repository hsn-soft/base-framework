using Microsoft.Extensions.Logging;
using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers.Storage;
using Hhs.Shared.Providers;

namespace Hhs.Shared.Providers.Storage;

/// <summary>
/// Generic HTTP-based CDN storage provider.
/// Can work with any HTTP endpoint that supports /upload and /download operations.
/// </summary>
public sealed class HttpCdnStorageProvider : ICdnStorageProvider
{
    private readonly IHasCdnBaseUrl _cdnSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpCdnStorageProvider> _logger;

    public HttpCdnStorageProvider(
        IHasCdnBaseUrl cdnSettings,
        HttpClient httpClient,
        ILogger<HttpCdnStorageProvider> logger)
    {
        _cdnSettings = cdnSettings;
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
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file '{Filename}' to HTTP CDN endpoint",
                    filename);
            }

            // Create multipart form data
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", filename);

            // Upload to HTTP endpoint
            var uploadUrl = $"{_cdnSettings.BaseUrl.TrimEnd('/')}/upload";
            var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"HTTP CDN upload failed with status {response.StatusCode}: " +
                    $"{await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            // Parse response
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            // Extract fileId from response
            var fileId = root.TryGetProperty("fileId", out var fileIdElement)
                ? fileIdElement.GetString()
                : root.TryGetProperty("fileName", out var fileNameElement)
                    ? fileNameElement.GetString()
                    : throw new InvalidOperationException("No fileId in upload response");

            // Construct storage and CDN URLs
            var storageUrl = $"{_cdnSettings.BaseUrl.TrimEnd('/')}/download/{fileId}";
            var cdnUrl = $"{_cdnSettings.BaseUrl.TrimEnd('/')}/{_cdnSettings.ZonePath.Trim('/')}/{_cdnSettings.PathPrefix.Trim('/')}/{fileId}"
                .Replace("//", "/")
                .Replace(":///", "://");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded via HTTP CDN. FileId: {FileId}, StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    fileId,
                    storageUrl,
                    cdnUrl);
            }

            return (storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to HTTP CDN", filename);
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
                    "Downloading file from HTTP CDN. Url: {StorageUrl}",
                    storageUrl);
            }

            var response = await _httpClient.GetAsync(storageUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"HTTP CDN download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from HTTP CDN URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
