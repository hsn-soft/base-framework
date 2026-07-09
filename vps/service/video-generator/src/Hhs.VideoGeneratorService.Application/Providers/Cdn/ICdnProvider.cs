using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Providers.Cdn;

public sealed class CdnUploadResult
{
    public bool IsFailed { get; set; }

    /// <summary>Only meaningful when IsFailed is true — set by the provider's own error classification.</summary>
    public bool IsRetryable { get; set; }

    [CanBeNull] public string ErrorMessage { get; set; }

    public string StorageUrl { get; set; } = default!;
    public string CdnUrl { get; set; } = default!;

    public CdnUploadResult() { }

    public CdnUploadResult(string storageUrl, string cdnUrl)
    {
        StorageUrl = storageUrl;
        CdnUrl = cdnUrl;
    }

    public void Deconstruct(out string storageUrl, out string cdnUrl)
    {
        storageUrl = StorageUrl;
        cdnUrl = CdnUrl;
    }
}

public sealed class CdnDownloadResult
{
    public bool IsFailed { get; set; }

    /// <summary>Only meaningful when IsFailed is true — set by the provider's own error classification.</summary>
    public bool IsRetryable { get; set; }

    [CanBeNull] public string ErrorMessage { get; set; }

    [CanBeNull] public Stream Content { get; set; }
}

public interface ICdnProvider
{
    string ProviderKey { get; }

    Task<CdnUploadResult> UploadAsync(
        Stream fileStream,
        string filename);

    Task<CdnDownloadResult> DownloadAsync(
        string storageUrl);
}
