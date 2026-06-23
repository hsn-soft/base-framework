namespace Hhs.MockApi.CdnLocalMinio.Options;

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = default!;
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public bool UseSsl { get; set; }
    public string ObjectKeyDateFormat { get; set; } = "yyyy-MM-dd";
    public string CacheControl { get; set; } = "public, max-age=31536000, immutable";
}
