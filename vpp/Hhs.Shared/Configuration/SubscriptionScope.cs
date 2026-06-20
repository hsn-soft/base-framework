namespace Hhs.Shared.Configuration;

/// <summary>
/// Subscription scope configuration containing provider keys for a specific scope.
/// Example: { "outline-fast", "audio-quick", "video-fast-external" }
/// </summary>
public sealed class SubscriptionScope
{
    public Guid ScopeKey { get; set; }

    public string OutlineProviderKey { get; set; } = default!;
    public string AudioProviderKey { get; set; } = default!;
    public string VideoProviderKey { get; set; } = default!;

    public SubscriptionScope()
    {
    }

    public SubscriptionScope(Guid scopeKey, string outlineProviderKey, string audioProviderKey, string videoProviderKey)
    {
        ScopeKey = scopeKey;
        OutlineProviderKey = outlineProviderKey;
        AudioProviderKey = audioProviderKey;
        VideoProviderKey = videoProviderKey;
    }
}
