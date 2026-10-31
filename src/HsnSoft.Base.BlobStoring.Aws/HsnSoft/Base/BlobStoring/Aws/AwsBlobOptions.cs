namespace HsnSoft.Base.BlobStoring.Aws;

/// <summary>Connection settings; section <c>AwsS3</c>. On AWS give the region and nothing else: the credentials are found the way the AWS SDK finds them (the role of the host, the environment, the profile). A store with the S3 API (MinIO, LocalStack) is reached with <see cref="ServiceUrl"/>.</summary>
public sealed class AwsBlobOptions
{
    public const string SectionName = "AwsS3";

    /// <summary>The region of the buckets (<c>eu-central-1</c>). Not needed with <see cref="ServiceUrl"/>.</summary>
    public string? Region { get; set; }

    /// <summary>The address of an S3-compatible store (<c>http://localhost:9000</c>); empty for AWS itself.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>The access key; empty on AWS (the role of the host is used). It goes with <see cref="SecretKey"/>: keep both in the environment, not in a file.</summary>
    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    /// <summary>Put the bucket in the path (<c>http://host/bucket/key</c>) instead of the host name, which an S3-compatible store on one address needs. Default <see langword="false"/> (it is on when <see cref="ServiceUrl"/> is given and this is not set).</summary>
    public bool? ForcePathStyle { get; set; }

    /// <summary>
    /// How long one request may take **in all**, an upload or a download included (a stored file of 160 MB did not finish in 2 seconds, and a slow connection takes minutes for a large one). Not set: the SDK's own, which has no limit,
    /// so a call to a server that does not answer waits for ever. Set it to what the largest file you store needs at the slowest connection you expect, plus some; the request is then ended and the call fails.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>How many times a request that failed in a way that may pass (a throttled request, a 5xx, a broken connection) is tried again, with a growing wait. Not set: the SDK's (2).</summary>
    public int? MaxErrorRetry { get; set; }
}
