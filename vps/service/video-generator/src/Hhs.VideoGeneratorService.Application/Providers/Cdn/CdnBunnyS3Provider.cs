using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Retry;
using Hhs.VideoGeneratorService.Domain.Configuration.Providers.Cdn;

namespace Hhs.VideoGeneratorService.Application.Providers.Cdn;

/// <summary>
/// Bunny.net's S3-compatible storage backing (e.g. BackBlaze B2), uploaded to via the real AWS S3
/// SDK (proper SigV4 signing) rather than a raw unsigned PUT. A Bunny Pull Zone in front of the
/// bucket makes the uploaded file publicly reachable.
/// </summary>
public sealed class CdnBunnyS3Provider : ICdnProvider
{
    private readonly CdnBunnyS3Settings _settings;
    private readonly IAmazonS3 _s3Client;

    public CdnBunnyS3Provider(CdnBunnyS3Settings settings)
    {
        _settings = settings;
        _s3Client = new AmazonS3Client(
            new BasicAWSCredentials(settings.ApiKey, settings.ApiSecret),
            new AmazonS3Config
            {
                ServiceURL = settings.BaseUrl,
                // Most non-AWS S3-compatible providers (BackBlaze B2 included) need path-style
                // requests (https://endpoint/bucket/key) rather than AWS's default virtual-hosted
                // style (https://bucket.endpoint/key).
                ForcePathStyle = true
            });
    }

    public CdnBunnyS3Provider(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public string ProviderKey => ProviderKeys.CdnBunnyS3;

    public async Task<CdnUploadResult> UploadAsync(Stream fileStream, string filename)
    {
        try
        {
            string objectKey = BuildObjectKey(_settings.Path, filename);

            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey,
                InputStream = fileStream,
                AutoCloseStream = false,
                ContentType = "application/octet-stream"
            };

            var response = await _s3Client.PutObjectAsync(request);
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                return new CdnUploadResult { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(response.HttpStatusCode), ErrorMessage = $"BackBlaze S3 upload failed: HTTP {(int)response.HttpStatusCode}" };
            }

            string storageUrl = $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.BucketName}/{objectKey}";
            string cdnHost = $"{_settings.BucketName}{_settings.PullZoneUrl}".TrimEnd('/');
            string cdnUrl = $"https://{cdnHost}/{objectKey}";

            return new CdnUploadResult(storageUrl, cdnUrl);
        }
        catch (AmazonServiceException ex)
        {
            return new CdnUploadResult { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(ex.StatusCode), ErrorMessage = ex.Message };
        }
        catch (Exception ex) when (ex is AmazonClientException || ExceptionClassifier.IsRetryable(ex))
        {
            // AmazonClientException covers transport-level failures (connection refused, DNS, etc.)
            // that the AWS SDK didn't get far enough to attach an HTTP status code to.
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
            string key = ExtractObjectKey(storageUrl);

            var response = await _s3Client.GetObjectAsync(_settings.BucketName, key);
            return new CdnDownloadResult { Content = response.ResponseStream };
        }
        catch (AmazonServiceException ex)
        {
            return new CdnDownloadResult { IsFailed = true, IsRetryable = ExceptionClassifier.IsRetryable(ex.StatusCode), ErrorMessage = ex.Message };
        }
        catch (Exception ex) when (ex is AmazonClientException || ExceptionClassifier.IsRetryable(ex))
        {
            return new CdnDownloadResult { IsFailed = true, IsRetryable = true, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new CdnDownloadResult { IsFailed = true, IsRetryable = false, ErrorMessage = ex.Message };
        }
    }

    private string ExtractObjectKey(string storageUrl)
    {
        string path = new Uri(storageUrl).AbsolutePath.TrimStart('/');
        string bucketPrefix = $"{_settings.BucketName}/";
        return path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase) ? path[bucketPrefix.Length..] : path;
    }

    private static string BuildObjectKey(string pathPrefix, string filename)
    {
        string trimmedPrefix = pathPrefix?.Trim('/') ?? "";
        return trimmedPrefix.Length > 0 ? $"{trimmedPrefix}/{filename}" : filename;
    }
}