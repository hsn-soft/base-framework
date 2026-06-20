using Microsoft.Extensions.Logging;
using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers;
using Hhs.Shared.Providers;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Storage;

namespace Hhs.VideoGeneratorService.Providers.Cdn;

/// <summary>
/// Local file system CDN storage provider implementation.
/// Stores files on the local disk and constructs CDN URLs for public access.
/// </summary>
public sealed class LocalCdnProvider : ICdnProvider
{
    private readonly IHasCdnBaseUrl _cdnSettings;
    private readonly LocalStorageSettings _storageSettings;
    private readonly ILogger<LocalCdnProvider> _logger;

    public LocalCdnProvider(
        IHasCdnBaseUrl cdnSettings,
        LocalStorageSettings storageSettings,
        ILogger<LocalCdnProvider> logger)
    {
        _cdnSettings = cdnSettings;
        _storageSettings = storageSettings;
        _logger = logger;
    }

    public async Task<(string StorageUrl, string CdnUrl)> UploadAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure storage directory exists
            var storagePath = _storageSettings.LocalPath ?? "/tmp/cdn-storage";
            var directoryPath = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Create subdirectory for date-based organization
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var fullDirectory = Path.Combine(storagePath, dateFolder);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            // Generate unique filename to avoid collisions
            var fileExtension = Path.GetExtension(filename);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var uniqueFilename = $"{fileNameWithoutExtension}_{Guid.NewGuid():N}{fileExtension}";
            var fullFilePath = Path.Combine(fullDirectory, uniqueFilename);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file to local storage. Original: {OriginalFilename}, Path: {FilePath}",
                    filename,
                    fullFilePath);
            }

            // Write file to disk
            await using (var fileStream2 = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileStream2, cancellationToken);
            }

            // Create storage URL (internal, file:// protocol)
            var storageUrl = new Uri(new FileInfo(fullFilePath).FullName, UriKind.Absolute).AbsoluteUri;

            // Create CDN URL (public)
            var relativePath = Path.Combine(dateFolder, uniqueFilename).Replace("\\", "/");
            var cdnUrl = $"{_cdnSettings.BaseUrl.TrimEnd('/')}/{_cdnSettings.ZonePath.Trim('/')}/{_cdnSettings.PathPrefix.Trim('/')}/{relativePath}"
                .Replace("//", "/")
                .Replace(":///", "://");

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File uploaded successfully. StorageUrl: {StorageUrl}, CdnUrl: {CdnUrl}",
                    storageUrl,
                    cdnUrl);
            }

            return (storageUrl, cdnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file '{Filename}' to local storage", filename);
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
                    "Downloading file from local storage. StorageUrl: {StorageUrl}",
                    storageUrl);
            }

            // Parse file:// URL to get local path
            var uri = new Uri(storageUrl);
            var filePath = uri.LocalPath;

            // Validate file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found at path: {filePath}");
            }

            // Return file stream
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await Task.FromResult(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from storage URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
