namespace HsnSoft.Base.BlobStoring.Minio;

/// <summary>Connection settings; section <c>Minio</c>.</summary>
public sealed class MinioOptions
{
    /// <summary>Host and port without a scheme, e.g. <c>localhost:9000</c>.</summary>
    public string Endpoint { get; set; } = "localhost:9000";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    /// <summary>
    /// How long one request may take in all, an upload or a download included. Not set: the one of the client. Set it to what the largest file you store needs at the slowest connection you expect, plus some: a request that takes
    /// longer is ended and the call fails, and a download that is read slowly counts the time the reader takes.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}
