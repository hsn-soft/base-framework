using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

public sealed class CdnAbcProvider : ICdnProvider
{
    public string ProviderKey => ProviderKeys.CdnAbc;
    private readonly CdnAbcSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnAbcProvider> _logger;

    public CdnAbcProvider(
        CdnAbcSettings settings,
        HttpClient httpClient,
        ILogger<CdnAbcProvider> logger)
    {
        _settings = settings;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CdnUploadResult> UploadAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file '{Filename}' to ABC CDN endpoint",
                    filename);
            }

            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", filename);

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_settings.BaseUrl.TrimEnd('/')}/api/cdn/assets/upload")
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(_settings.APIKey))
            {
                request.Headers.Add("X-Api-Key", _settings.APIKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"ABC CDN upload failed with status {response.StatusCode}: " +
                    $"{await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            string? objectKey = root.TryGetProperty("objectKey", out var objectKeyElement)
                ? objectKeyElement.GetString()
                : null;

            if (objectKey is null)
                throw new InvalidOperationException("No objectKey in upload response");

            string? cdnUrl = root.TryGetProperty("cdnUrl", out var cdnUrlElement)
                ? cdnUrlElement.GetString()
                : null;

            if (cdnUrl is null)
                throw new InvalidOperationException("No cdnUrl in upload response");

            // ABC CDN uses external storage, so StorageUrl points to StorageXyz API
            var storageUrl = $"{_settings.StorageApiBaseUrl.TrimEnd('/')}/api/storage/download?key={Uri.EscapeDataString(objectKey)}";

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded to ABC CDN. ObjectKey: {ObjectKey}, StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    objectKey,
                    storageUrl,
                    cdnUrl);
            }

            return new CdnUploadResult(storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to ABC CDN", filename);
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
                    "Downloading file from ABC CDN. Url: {StorageUrl}",
                    storageUrl);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, storageUrl);
            
            if (!string.IsNullOrEmpty(_settings.StorageApiKey))
            {
                request.Headers.Add("X-Api-Key", _settings.StorageApiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"ABC CDN download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from ABC CDN URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
