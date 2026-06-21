using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

public sealed class CdnAbcCloudFrontProvider : ICdnProvider
{
    public string ProviderKey => ProviderKeys.CdnAbcCloudFront;
    private readonly CdnAbcCloudFrontSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnAbcCloudFrontProvider> _logger;

    public CdnAbcCloudFrontProvider(
        CdnAbcCloudFrontSettings settings,
        HttpClient httpClient,
        ILogger<CdnAbcCloudFrontProvider> logger)
    {
        _settings = settings;
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
                    "Uploading file '{Filename}' to ABC CloudFront CDN endpoint",
                    filename);
            }

            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", filename);

            string uploadUrl = $"{_settings.BaseUrl.TrimEnd('/')}/upload";
            var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"ABC CloudFront CDN upload failed with status {response.StatusCode}: " +
                    $"{await response.Content.ReadAsStringAsync(cancellationToken)}");
            }

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseJson);
            var root = jsonDoc.RootElement;

            string? fileId = root.TryGetProperty("fileId", out var fileIdElement)
                ? fileIdElement.GetString()
                : root.TryGetProperty("fileName", out var fileNameElement)
                    ? fileNameElement.GetString()
                    : throw new InvalidOperationException("No fileId in upload response");

            string storageUrl = $"{_settings.BaseUrl.TrimEnd('/')}/download/{fileId}";
            string cdnUrl = $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.ZonePath.Trim('/')}/{_settings.PathPrefix.Trim('/')}/{fileId}"
                .Replace("//", "/")
                .Replace(":///", "://");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded to ABC CloudFront CDN. FileId: {FileId}, StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    fileId,
                    storageUrl,
                    cdnUrl);
            }

            return (storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to ABC CloudFront CDN", filename);
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
                    "Downloading file from ABC CloudFront CDN. Url: {StorageUrl}",
                    storageUrl);
            }

            var response = await _httpClient.GetAsync(storageUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"ABC CloudFront CDN download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from ABC CloudFront CDN URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
