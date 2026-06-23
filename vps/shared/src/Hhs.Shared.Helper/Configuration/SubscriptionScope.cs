namespace Hhs.Shared.Helper.Configuration;

/// <summary>
/// Subscription scope configuration containing provider keys for a specific scope.
/// Example: { "outline-fast", "audio-quick", "video-fast-external" }
/// </summary>
public sealed class SubscriptionScope
{
    public string OutlineProviderKey { get; }
    public string? AudioProviderKey { get; }
    public string VideoProviderKey { get; }

    public SubscriptionScope(string outlineProviderKey, string? audioProviderKey, string videoProviderKey)
    {
        OutlineProviderKey = outlineProviderKey;
        AudioProviderKey = audioProviderKey;
        VideoProviderKey = videoProviderKey;
    }
}
