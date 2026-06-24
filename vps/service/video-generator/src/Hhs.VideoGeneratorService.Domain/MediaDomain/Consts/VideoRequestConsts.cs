namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Consts;

public static class VideoRequestConsts
{
    public const string TableName = "video_requests";
    public const int ScopeKeyMaxLength = 100;
    public const int StatusMaxLength = 80;
    public const int CurrentStepMaxLength = 100;
    public const int MediaInputJsonMaxLength = 5000;
    public const int AudioProviderKeyMaxLength = 100;
    public const int VideoProviderKeyMaxLength = 100;
    public const int VideoProviderTrackingIdMaxLength = 500;
    public const int VideoProviderUrlMaxLength = 2000;
    public const int VideoLocalPathMaxLength = 1000;
    public const int VideoCdnProviderKeyMaxLength = 100;
    public const int VideoStorageUrlMaxLength = 2000;
    public const int VideoCdnUrlMaxLength = 2000;
    public const int LastErrorMaxLength = 1000;
}
