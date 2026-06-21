namespace Hhs.VideoGeneratorService.Providers.Cdn;

/// <summary>
/// Provider abstraction for CDN operations.
/// Each CDN provider (Local, S3, Cloudflare, etc.) implements this interface.
/// </summary>
public interface ICdnProvider
{
    /// <summary>
    /// Uploads a file to CDN storage.
    /// </summary>
    /// <param name="fileStream">File stream to upload</param>
    /// <param name="filename">Filename to save as</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (StorageUrl, CdnUrl) - StorageUrl is backend storage URL (with credentials), CdnUrl is public CDN URL</returns>
    Task<(string StorageUrl, string CdnUrl)> UploadAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a file from CDN storage.
    /// </summary>
    /// <param name="storageUrl">Storage URL returned from UploadAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    Task<Stream> DownloadAsync(
        string storageUrl,
        CancellationToken cancellationToken);
}
