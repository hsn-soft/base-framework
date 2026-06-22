using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

public sealed class CdnBunnySelfProvider : ICdnProvider
{
    public string ProviderKey => ProviderKeys.CdnBunnySelf;
    private readonly CdnBunnySelfSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CdnBunnySelfProvider> _logger;

    public CdnBunnySelfProvider(
        CdnBunnySelfSettings settings,
        HttpClient httpClient,
        ILogger<CdnBunnySelfProvider> logger)
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
                    "Uploading file '{Filename}' to Bunny Self CDN endpoint",
                    filename);
            }

            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", filename);

            string uploadUrl = $"{_settings.BaseUrl.TrimEnd('/')}/upload";
            var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Bunny Self CDN upload failed with status {response.StatusCode}: " +
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
                    "File uploaded to Bunny Self CDN. FileId: {FileId}, StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    fileId,
                    storageUrl,
                    cdnUrl);
            }

            return new CdnUploadResult(storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to Bunny Self CDN", filename);
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
                    "Downloading file from Bunny Self CDN. Url: {StorageUrl}",
                    storageUrl);
            }

            var response = await _httpClient.GetAsync(storageUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Bunny Self CDN download failed with status {response.StatusCode}");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from Bunny Self CDN URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
