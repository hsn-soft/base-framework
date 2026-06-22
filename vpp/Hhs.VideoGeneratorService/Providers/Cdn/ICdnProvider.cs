namespace Hhs.VideoGeneratorService.Providers.Cdn;

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
