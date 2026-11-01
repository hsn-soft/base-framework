namespace Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Consts;

public static class VideoRequestConsts
{
    public const string TableName = "VideoRequest";
    public const int ScopeKeyMaxLength = 100;
    public const int RefContentTypeMaxLength = 30;
    public const int StatusMaxLength = 50;
    public const int CurrentMilestoneMaxLength = 50;
    public const int MediaInputJsonMaxLength = 8000;
    public const int AudioProviderKeyMaxLength = 50;
    public const int VideoProviderKeyMaxLength = 50;
    public const int VideoProviderTrackingIdMaxLength = 100;
    public const int VideoProviderUrlMaxLength = 500;
    public const int VideoStorageUrlMaxLength = 500;
    public const int VideoCdnUrlMaxLength = 500;
    public const int LastErrorMaxLength = 1000;
}
