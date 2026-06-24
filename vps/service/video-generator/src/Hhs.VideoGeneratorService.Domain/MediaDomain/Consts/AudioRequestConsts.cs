namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Consts;

public static class AudioRequestConsts
{
    public const string TableName = "audio_requests";
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
