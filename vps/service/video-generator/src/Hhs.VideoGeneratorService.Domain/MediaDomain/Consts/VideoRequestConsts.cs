using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Consts;

public static class VideoRequestConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(VideoRequest.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "VideoRequests";
    public const int ScopeKeyMaxLength = 100;
    public const int CorrelationIdMaxLength = 50;
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
