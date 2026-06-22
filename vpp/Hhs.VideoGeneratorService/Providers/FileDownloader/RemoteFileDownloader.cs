using Microsoft.Extensions.Options;
using Hhs.VideoGeneratorService.Configuration;

namespace Hhs.VideoGeneratorService.Providers.FileDownloader;

public sealed class RemoteFileDownloader(
    IOptions<SystemCdnSettings> cdnSettings,
    ILogger<RemoteFileDownloader> logger,
    HttpClient httpClient) : IRemoteFileDownloader
{
    private readonly SystemCdnSettings _cdnSettings = cdnSettings.Value;

    public async Task<(bool Success, string Result)> DownloadAsync(
        string remoteUrl,
        string extension,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(remoteUrl))
            {
                return (false, "Remote URL is required");
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                return (false, "File extension is required");
            }

            // Ensure download directory exists
            string downloadDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", _cdnSettings.LocalDownloadPath);
            var dirInfo = Directory.CreateDirectory(downloadDir);

            // Generate filename
            string filename = $"{Guid.CreateVersion7().ToString("N").ToLower()}.{extension}";
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

                logger.LogInformation("File downloaded successfully: {Filename} from {RemoteUrl}", filename, remoteUrl);
                return (true, filename);
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
