using Microsoft.Extensions.Options;
using Hhs.VideoGeneratorService.Configuration;

namespace Hhs.VideoGeneratorService.Providers.FileDownloader;

public sealed class RemoteFileDownloader(
    IOptions<SystemCdnSettings> cdnSettings,
    ILogger<RemoteFileDownloader> logger,
    HttpClient httpClient,
    IHostEnvironment environment)
    : IRemoteFileDownloader
{
    private readonly SystemCdnSettings _cdnSettings = cdnSettings.Value ?? throw new ArgumentNullException(nameof(cdnSettings));

    public async Task<(bool Success, string Result)> DownloadAsync(string remoteUrl, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(remoteUrl))
            {
                return (false, "Remote URL is required");
            }

            // Extract filename and extension from URL
            var uri = new Uri(remoteUrl);
            string filename = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = $"{Guid.CreateVersion7().ToString("N").ToLower()}";
            }

            // Build download directory path
            string downloadDir = _cdnSettings.LocalDownloadPath;

            // If development environment, prepend relative path components
            if (environment.IsDevelopment())
            {
                downloadDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", downloadDir);
            }

            var dirInfo = Directory.CreateDirectory(downloadDir);
            string filePath = Path.Combine(dirInfo.FullName, filename);

            try
            {
                // Download file
                using (var response = await httpClient.GetAsync(remoteUrl, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, $"Failed to download file: HTTP {response.StatusCode}");
                    }

                    using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                    {
                        using (var fileStream = System.IO.File.Create(filePath))
                        {
                            await contentStream.CopyToAsync(fileStream, cancellationToken);
                        }
                    }
                }

                logger.LogInformation("File downloaded successfully: {FilePath} from {RemoteUrl}", filePath, remoteUrl);
                return (true, filePath);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                // Clean up partial file if exists
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                string errorMessage = $"Failed to save file to disk: {ex.Message}";
                logger.LogError(ex, errorMessage);
                return (false, errorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            return (false, "Download was cancelled");
        }
        catch (Exception ex)
        {
            string errorMessage = $"Unexpected error during download: {ex.Message}";
            logger.LogError(ex, errorMessage);
            return (false, errorMessage);
        }
    }
}
