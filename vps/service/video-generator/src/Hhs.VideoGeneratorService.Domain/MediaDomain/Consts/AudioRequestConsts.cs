using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Consts;

public static class AudioRequestConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(AudioRequest.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "AudioRequests";
    public const int ScopeKeyMaxLength = 100;
    public const int CorrelationIdMaxLength = 50;
    public const int StatusMaxLength = 80;
    public const int CurrentStepMaxLength = 100;
    public const int InputTextMaxLength = 5000;
    public const int AudioProviderKeyMaxLength = 100;
    public const int AudioProviderTrackingIdMaxLength = 500;
    public const int AudioProviderUrlMaxLength = 2000;
    public const int AudioLocalPathMaxLength = 1000;
    public const int AudioStorageUrlMaxLength = 2000;
    public const int AudioCdnUrlMaxLength = 2000;
    public const int AudioCdnProviderKeyMaxLength = 100;
    public const int LastErrorMaxLength = 1000;
}
