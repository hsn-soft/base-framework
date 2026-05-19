namespace Hhs.AuthServer.Options;

public sealed class AuthServerPolicyOptions
{
    public string[] AllowedCorsOrigins { get; set; } = [];
    public string[] TrustedProxyIps { get; set; } = [];
    public string[] DocsInternalCidrs { get; set; } = [];
    public string[] DocsAllowedRoles { get; set; } = ["Admin", "BackOfficeAdmin"];
    public bool DocsRequireAuthenticatedUser { get; set; } = true;
    public bool DocsAllowInternalAnonymousAccess { get; set; } = true;

    public KestrelPolicyOptions Kestrel { get; set; } = new();
    public RequestBodyLimitOptions RequestBodyLimits { get; set; } = new();
    public RateLimitPolicyOptions RateLimits { get; set; } = new();
    public ConcurrencyPolicyOptions Concurrency { get; set; } = new();
    public RouteMatchOptions Routes { get; set; } = new();
}

public sealed class KestrelPolicyOptions
{
    public long MaxRequestBodySizeBytes { get; set; } = 104857600; // 100 MB hard ceiling
    public int RequestHeadersTimeoutSeconds { get; set; } = 15;
    public int KeepAliveTimeoutSeconds { get; set; } = 60;
    public int MaxRequestHeaderCount { get; set; } = 64;
    public int MaxRequestHeadersTotalSizeBytes { get; set; } = 32768; // 32 KB
}

public sealed class RequestBodyLimitOptions
{
    public long DefaultBytes { get; set; } = 262144; // 256 KB
    public long IdentityLoginBytes { get; set; } = 65536; // 64 KB
    public long ContentBytes { get; set; } = 524288; // 512 KB
    public long TextNormalizerUploadBytes { get; set; } = 26214400; // 25 MB
    public long VideoUploadBytes { get; set; } = 104857600; // 100 MB
    public long DownloadBytes { get; set; } = 65536; // GET için genelde body yok
}

public sealed class RateLimitPolicyOptions
{
    public int GlobalPerMinute { get; set; } = 300;
    public int SwaggerPerMinute { get; set; } = 20;

    public int IdentityLoginPerMinute { get; set; } = 5;
    public int IdentityAuthPerMinute { get; set; } = 20;

    public int ContentDefaultReadPerMinute { get; set; } = 120;
    public int ContentSearchPerMinute { get; set; } = 30;
    public int ContentExportPerMinute { get; set; } = 6;

    public int TextNormalizerPerMinute { get; set; } = 30;
    public int VideoGeneratorPerMinute { get; set; } = 20;

    public int QueueLimit { get; set; } = 0;
}

public sealed class ConcurrencyPolicyOptions
{
    public int TextNormalizerMaxConcurrent { get; set; } = 4;
    public int VideoGeneratorMaxConcurrent { get; set; } = 2;
}

public sealed class RouteMatchOptions
{
    public string[] DocsPrefixes { get; set; } = ["/docs", "/swagger", "/openapi-proxy"];
    public string[] IdentityLoginPaths { get; set; } = ["/identity-service/v1/auth/login"];
    public string[] IdentityAuthPrefixes { get; set; } = ["/identity-service/v1/auth"];
    public string[] ContentSearchPrefixes { get; set; } = ["/content-service/v1/search"];
    public string[] ContentExportPrefixes { get; set; } = ["/content-service/v1/export"];
    public string[] ContentDefaultReadPrefixes { get; set; } =
    [
        "/content-service/v1/articles",
        "/content-service/v1/categories",
        "/content-service/v1/tags"
    ];

    public string[] TextNormalizerPrefixes { get; set; } = ["/text-normalizer-service/v1"];
    public string[] TextNormalizerUploadPrefixes { get; set; } = ["/text-normalizer-service/v1/upload"];
    public string[] TextNormalizerDownloadPrefixes { get; set; } = ["/text-normalizer-service/v1/download"];

    public string[] VideoGeneratorPrefixes { get; set; } = ["/video-generator-service/v1"];
    public string[] VideoGeneratorUploadPrefixes { get; set; } = ["/video-generator-service/v1/upload"];
    public string[] VideoGeneratorDownloadPrefixes { get; set; } = ["/video-generator-service/v1/download"];
}