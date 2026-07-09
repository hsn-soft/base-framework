namespace Hhs.VideoGeneratorService.Application.Providers.FileDownloader;

public interface IRemoteFileDownloader
{
    /// <summary>
    /// Downloads a file from remote URL to local storage.
    /// </summary>
    /// <param name="providerKey">The provider key used as a filename prefix.</param>
    /// <param name="remoteUrl">The remote URL to download from</param>
    /// <returns>
    /// (bool Success, string Result, bool IsRetryable)
    /// - Success=true: Result contains full file path (e.g., "/path/to/downloads/audio-quick-xxx.mp3")
    /// - Success=false: Result contains error message; IsRetryable classifies the failure (timeout/network
    ///   vs. permanent) so the caller doesn't have to re-derive it from a collapsed error string.
    /// </returns>
    Task<(bool Success, string Result, bool IsRetryable)> DownloadAsync(string remoteUrl);
}
