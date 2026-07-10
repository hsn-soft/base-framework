namespace Hhs.VideoGeneratorService.Domain.Constants;

public static class ErrorMessages
{
    public const string ProviderKeyValueUnknown = "Provider key value is unknown. Scope key:";

    public const string AudioProviderPollingTimeout = "Audio provider polling timeout.";
    public const string AudioProviderFailed = "Audio provider failed.";

    public const string VideoProviderPollingTimeout = "Video provider polling timeout.";
    public const string VideoProviderFailed = "Video provider failed.";

    public const string AudioProviderFailedNoUrl = "Audio provider completed but file url is empty.";
    public const string VideoProviderFailedNoUrl = "Video provider completed but file url is empty.";
    public const string AudioProviderTrackIdRequired = "Audio provider track id is required.";
    public const string VideoProviderTrackIdRequired = "Video provider track id is required.";

    public const string AudioProviderKeyRequired = "AudioProviderKey is required.";
    public const string CdnProviderKeyRequired = "Cdn Provider Key is required.";
    public const string AudioProviderUrlRequired = "Audio provider url is required.";
    public const string AudioLocalPathRequired = "Audio local path is required.";
    public const string AudioLocalFileNotExist = "Audio local file is not exist:";
    public const string VideoLocalPathRequired = "Video Local Path is required.";
}
