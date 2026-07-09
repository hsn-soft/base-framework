using System.Net.Http.Headers;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Application.Providers.Cdn;

/// <summary>
/// Real Bunny.net Edge Storage API (PUT/GET https://storage.bunnycdn.com/{StorageZoneName}/{path}
/// with an "AccessKey" header) — not a mock. The Pull Zone in front of the storage zone is what
/// makes the uploaded file publicly reachable (needed by external services like Creatomate that
/// can't reach a local dev machine).
/// </summary>
public sealed class CdnBunnySelfProvider(CdnBunnySelfSettings settings, HttpClient httpClient) : ICdnProvider
{
    public string ProviderKey => ProviderKeys.CdnBunnySelf;

    public async Task<CdnUploadResult> UploadAsync(Stream fileStream, string filename)
    {
        try
        {
            string objectKey = BuildObjectKey(settings.Path, filename);

            using var request = new HttpRequestMessage(HttpMethod.Put, $"{settings.BaseUrl.TrimEnd('/')}/{settings.StorageZoneName.Trim('/')}/{objectKey}");
            request.Content = new StreamContent(fileStream);
            request.Headers.Add("AccessKey", settings.ApiKey);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new CdnUploadResult { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"Bunny storage upload failed: HTTP {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}" };
            }

            string storageUrl = $"{settings.BaseUrl.TrimEnd('/')}/{settings.StorageZoneName.Trim('/')}/{objectKey}";
            string cdnHost = $"{settings.StorageZoneName.Trim('/')}{settings.PullZoneUrl}".TrimEnd('/');
            string cdnUrl = $"https://{cdnHost}/{objectKey}";

            return new CdnUploadResult(storageUrl, cdnUrl);
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new CdnUploadResult { IsFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new CdnUploadResult { IsFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<CdnDownloadResult> DownloadAsync(string storageUrl)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, storageUrl);
            request.Headers.Add("AccessKey", settings.ApiKey);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return new CdnDownloadResult { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.StatusCode), ErrorMessage = $"Bunny storage download failed: HTTP {(int)response.StatusCode}" };
            }

            return new CdnDownloadResult { Content = await response.Content.ReadAsStreamAsync() };
        }
        catch (Exception ex) when (ExceptionClassifier.IsRetryable(ex))
        {
            return new CdnDownloadResult { IsFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new CdnDownloadResult { IsFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }

    private static string BuildObjectKey(string pathPrefix, string filename)
    {
        string trimmedPrefix = pathPrefix?.Trim('/') ?? "";
        return trimmedPrefix.Length > 0 ? $"{trimmedPrefix}/{filename}" : filename;
    }
}