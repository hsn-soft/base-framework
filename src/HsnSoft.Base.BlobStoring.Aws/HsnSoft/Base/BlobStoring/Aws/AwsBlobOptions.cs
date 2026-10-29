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
}
