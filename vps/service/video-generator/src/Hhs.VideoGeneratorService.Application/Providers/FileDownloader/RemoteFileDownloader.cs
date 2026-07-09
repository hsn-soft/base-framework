using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hhs.VideoGeneratorService.Application.Providers.FileDownloader;

public sealed class RemoteFileDownloader(
    IOptions<SystemCdnSettings> cdnSettings,
    ILogger<RemoteFileDownloader> logger,
    HttpClient httpClient,
    IHostEnvironment environment
) : IRemoteFileDownloader
{
    private readonly SystemCdnSettings _cdnSettings = cdnSettings.Value;

    public async Task<(bool Success, string Result, bool IsRetryable)> DownloadAsync(string remoteUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(remoteUrl))
            {
                return (false, "Remote URL is required", false);
            }

            // Some providers (e.g. ElevenLabs, which returns audio bytes synchronously with no
            // separately-hosted URL) already persist the generated file to local disk themselves
            // during CreateAsync and hand back that local path as ProviderFileUrl. Detect that case
            // and short-circuit instead of attempting an HTTP GET against a filesystem path.
            bool isHttpUrl = Uri.TryCreate(remoteUrl, UriKind.Absolute, out var parsedUrl) &&
                             (parsedUrl.Scheme == Uri.UriSchemeHttp || parsedUrl.Scheme == Uri.UriSchemeHttps);

            if (!isHttpUrl)
            {
                if (File.Exists(remoteUrl))
                {
                    logger.LogInformation("File is already local, skipping download: {FilePath}", remoteUrl);
                    return (true, remoteUrl, false);
                }

                return (false, $"Remote URL is not a valid http(s) URL and no local file exists at: {remoteUrl}", false);
            }

            // Extract filename and extension from URL
            var uri = new Uri(remoteUrl);
            string filename = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = $"{Guid.CreateVersion7().ToString("N").ToLower()}";
            }

            filename = $"{filename}";

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
                using (var response = await httpClient.GetAsync(remoteUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, $"Failed to download file: HTTP {response.StatusCode}", ExceptionClassifier.IsRetryable(response.StatusCode));
                    }

                    await using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        await using (var fileStream = File.Create(filePath))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                }

                logger.LogInformation("File downloaded successfully: {FilePath} from {RemoteUrl}", filePath, remoteUrl);
                return (true, filePath, false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                // Clean up partial file if exists
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                string errorMessage = $"Failed to save file to disk: {ex.Message}";
                logger.LogError(ex, errorMessage);
                return (false, errorMessage, ExceptionClassifier.IsRetryable(ex));
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout/cancellation — the same class of transient failure ExceptionClassifier treats
            // TaskCanceledException as, just caught here via its OperationCanceledException base.
            return (false, "Download was cancelled", true);
        }
        catch (Exception ex)
        {
            string errorMessage = $"Unexpected error during download: {ex.Message}";
            logger.LogError(ex, errorMessage);
            return (false, errorMessage, ExceptionClassifier.IsRetryable(ex));
        }
    }
}