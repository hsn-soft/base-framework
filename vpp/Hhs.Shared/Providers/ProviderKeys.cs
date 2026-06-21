namespace Hhs.Shared.Providers;

public static class ProviderKeys
{
    // Outline Providers
    public const string OutlineFast = "outline-fast";
    public const string OutlineQueue = "outline-queue";

    // Audio Providers
    public const string AudioQuick = "audio-quick";
    public const string AudioHQ = "audio-hq";

    // Video Providers
    public const string VideoFastExternal = "video-fast-external";
    public const string VideoFastInternal = "video-fast-internal";
    public const string VideoQueueExternal = "video-queue-external";
    public const string VideoQueueInternal = "video-queue-internal";

    // CDN Providers (Storage)
    public const string CdnLocalMinio = "cdn-local-minio";
    public const string CdnBunnySelf = "cdn-bunny-self";
    public const string CdnBunnyS3 = "cdn-bunny-s3";
    public const string CdnAbcCloudFront = "cdn-abc-cloudfront";
}
