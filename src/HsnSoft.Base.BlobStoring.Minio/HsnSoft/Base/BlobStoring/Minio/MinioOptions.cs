namespace HsnSoft.Base.BlobStoring.Minio;

/// <summary>Connection settings; section <c>Minio</c>.</summary>
public sealed class MinioOptions
{
    /// <summary>Host and port without a scheme, e.g. <c>localhost:9000</c>.</summary>
    public string Endpoint { get; set; } = "localhost:9000";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; }
}
