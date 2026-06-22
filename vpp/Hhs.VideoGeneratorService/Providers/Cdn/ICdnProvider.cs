namespace Hhs.VideoGeneratorService.Providers.Cdn;

public sealed class CdnUploadResult
{
    public string StorageUrl { get; set; } = default!;
    public string CdnUrl { get; set; } = default!;
}

public interface ICdnProvider
{
    string ProviderKey { get; }

    Task<CdnUploadResult> UploadAsync(
        Stream fileStream,
        string filename,
        CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(
        string storageUrl,
        CancellationToken cancellationToken);
}
