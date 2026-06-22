namespace Hhs.VideoGeneratorService.Providers.Cdn;

public sealed class CdnUploadResult
{
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
