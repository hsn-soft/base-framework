using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Services;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to CDN storage and returns both storage and CDN URLs
    /// </summary>
    /// <param name="cdnProviderKey">The CDN provider key (LocalStorageCdn, S3Cdn, AzureCdn, etc.)</param>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="filename">The filename to save as</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (StorageUrl, CdnUrl)</returns>
    Task<(string StorageUrl, string CdnUrl)> UploadAsync(
        string cdnProviderKey,
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a file from storage using the storage URL (internal backend use)
    /// </summary>
    /// <param name="storageUrl">The storage URL returned from upload</param>
    /// <param name="cdnProviderKey">The CDN provider key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    Task<Stream> DownloadAsync(
        string storageUrl,
        string cdnProviderKey,
        CancellationToken cancellationToken);
}

public sealed class DummyStorageService : IStorageService
{
    private readonly ICdnProviderResolver _providerResolver;
    private readonly ILogger<DummyStorageService> _logger;

    public DummyStorageService(
        ICdnProviderResolver providerResolver,
        ILogger<DummyStorageService> logger)
    {
        _providerResolver = providerResolver;
        _logger = logger;
    }

    public async Task<(string StorageUrl, string CdnUrl)> UploadAsync(
        string cdnProviderKey,
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Uploading file '{Filename}' to CDN provider '{CdnProviderKey}'",
                    filename,
                    cdnProviderKey);
            }

            // Create provider instance for the given CDN key
            var provider = _providerResolver.Resolve(cdnProviderKey);

            // Upload using the provider
            (string storageUrl, string cdnUrl) = await provider.UploadAsync(fileStream, filename, cancellationToken);

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
            _logger.LogError(ex, "Failed to upload file '{Filename}' to CDN provider '{CdnProviderKey}'",
                filename, cdnProviderKey);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(
        string storageUrl,
        string cdnProviderKey,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Downloading file from storage URL '{StorageUrl}' using CDN provider '{CdnProviderKey}'",
                    storageUrl,
                    cdnProviderKey);
            }

            // Create provider instance for the given CDN key
            var provider = _providerResolver.Resolve(cdnProviderKey);

            // Download using the provider
            var stream = await provider.DownloadAsync(storageUrl, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "File downloaded successfully from CDN provider '{CdnProviderKey}'",
                    cdnProviderKey);
            }

            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from storage URL '{StorageUrl}'", storageUrl);
            throw;
        }
    }
}
