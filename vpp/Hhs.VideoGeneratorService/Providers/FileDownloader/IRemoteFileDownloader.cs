namespace Hhs.VideoGeneratorService.Providers.FileDownloader;

public interface IRemoteFileDownloader
{
    /// <summary>
    /// Downloads a file from remote URL to local storage.
    /// </summary>
    /// <param name="remoteUrl">The remote URL to download from</param>
    /// <param name="extension">File extension (e.g., "mp3", "avi")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// (bool Success, string Result)
    /// - Success=true: Result contains filename with extension (e.g., "audio-quick-xxx.mp3")
    /// - Success=false: Result contains error message
    /// </returns>
    Task<(bool Success, string Result)> DownloadAsync(string remoteUrl, string extension, CancellationToken cancellationToken);
}
