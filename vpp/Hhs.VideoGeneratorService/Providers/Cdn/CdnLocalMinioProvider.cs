using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

public sealed class CdnLocalMinioProvider(
    CdnLocalMinioSettings settings,
    HttpClient httpClient,
    ILogger<CdnLocalMinioProvider> logger) : ICdnProvider
{
    public string ProviderKey => ProviderKeys.CdnLocalMinio;

    public async Task<CdnUploadResult> UploadAsync(Stream fileStream, string filename, CancellationToken cancellationToken)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Uploading file '{Filename}' to LocalMinio CDN endpoint",
                    filename);
            }

            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", filename);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/api/cdn/assets/upload")
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(settings.APIKey))
            {
                request.Headers.Add("X-Api-Key", settings.APIKey);
            }

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"LocalMinio CDN upload failed with status {response.StatusCode}: " +
                    $"{await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            // Parse CDN's response format
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

            // Convert CDN response to standardized CdnUploadResult
            // Storage URL: Private access with API key (ApplicationLayer responsibility)
            var storageUrl = $"{settings.BaseUrl.TrimEnd('/')}/api/cdn/assets/download?key={Uri.EscapeDataString(objectKey)}";

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "File uploaded to LocalMinio CDN. ObjectKey: {ObjectKey}, StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    objectKey,
                    storageUrl,
                    cdnUrl);
            }

            return new CdnUploadResult(storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload file '{Filename}' to LocalMinio CDN", filename);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(
        string storageUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Downloading file from LocalMinio CDN. Url: {StorageUrl}",
                    storageUrl);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, storageUrl);

            if (!string.IsNullOrEmpty(settings.APIKey))
            {
                request.Headers.Add("X-Api-Key", settings.APIKey);
            }

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"LocalMinio CDN download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file from LocalMinio CDN URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
