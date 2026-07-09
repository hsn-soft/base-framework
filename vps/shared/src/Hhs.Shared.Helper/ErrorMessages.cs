namespace Hhs.Shared.Helper;

public static class ErrorMessages
{
    public const string OutlineProviderPollingTimeout = "Outline provider polling timeout.";
    public const string OutlineProviderFailed = "Outline provider failed.";

    public const string AudioProviderPollingTimeout = "Audio provider polling timeout.";
    public const string AudioProviderFailed = "Audio provider failed.";

    public const string VideoProviderPollingTimeout = "Video provider polling timeout.";
    public const string VideoProviderFailed = "Video provider failed.";

    public const string AudioProviderFailedNoUrl = "Audio provider completed but file url is empty.";
    public const string VideoProviderFailedNoUrl = "Video provider completed but file url is empty.";
    public const string AudioProviderTrackIdRequired = "Audio provider track id is required.";
    public const string VideoProviderTrackIdRequired = "Video provider track id is required.";
    public const string OutlineProviderFailedEmptyScript = "Outline provider completed but script is empty.";
}