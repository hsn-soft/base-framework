namespace Hhs.VideoGeneratorService.Providers.FileDownloader;

public interface IFileDownloader
{
    Task<string> DownloadAsync(string fileUrl, string extension, CancellationToken cancellationToken);
    Task<string> DownloadAsync(string fileUrl, string extension, string? providerFileName, CancellationToken cancellationToken);
}