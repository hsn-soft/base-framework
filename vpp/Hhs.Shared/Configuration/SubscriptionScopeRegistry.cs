namespace Hhs.Shared.Configuration;

/// <summary>
/// In-memory registry of subscription scopes and their provider configurations.
/// This acts as a mini-database for provider key lookups based on ScopeKey.
///
/// Instead of passing provider keys through events, we pass ScopeKey and lookup
/// the configuration here. This provides centralized management of subscriptions.
///
/// TODO: Replace with actual database queries when persistent storage is needed.
/// </summary>
public static class SubscriptionScopeRegistry
{
    private static readonly Dictionary<string, SubscriptionScope> _scopes = [];

    /// <summary>
    /// Initialize with 12 test scenarios matching HTTP test files.
    /// </summary>
    public static void Initialize()
    {
        _scopes.Clear();

        // Scenarios 1-4: outline-fast combinations
        Add("scenario-001", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: "audio-quick",
            videoProviderKey: "video-fast-external"
        ));

        Add("scenario-002", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: "audio-hq",
            videoProviderKey: "video-fast-external"
        ));

        Add("scenario-003", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: "audio-quick",
            videoProviderKey: "video-queue-external"
        ));

        Add("scenario-004", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: "audio-hq",
            videoProviderKey: "video-queue-external"
        ));

        // Scenarios 5-8: outline-queue combinations
        Add("scenario-005", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: "audio-quick",
            videoProviderKey: "video-fast-external"
        ));

        Add("scenario-006", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: "audio-hq",
            videoProviderKey: "video-fast-external"
        ));

        Add("scenario-007", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: "audio-quick",
            videoProviderKey: "video-queue-external"
        ));

        Add("scenario-008", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: "audio-hq",
            videoProviderKey: "video-queue-external"
        ));

        // Scenarios 9-12: internal video combinations
        Add("scenario-009", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: null,
            videoProviderKey: "video-fast-internal"
        ));

        Add("scenario-010", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: null,
            videoProviderKey: "video-fast-internal"
        ));

        Add("scenario-011", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: null,
            videoProviderKey: "video-queue-internal"
        ));

        Add("scenario-012", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: null,
            videoProviderKey: "video-queue-internal"
        ));
    }

    /// <summary>
    /// Get provider configuration for a specific scope.
    /// </summary>
    public static SubscriptionScope? GetScope(string scopeKey)
    {
        return _scopes.TryGetValue(scopeKey, out var scope) ? scope : null;
    }

    /// <summary>
    /// Get outline provider key for a scope.
    /// </summary>
    public static string? GetOutlineProviderKey(string scopeKey)
    {
        return GetScope(scopeKey)?.OutlineProviderKey;
    }

    /// <summary>
    /// Get audio provider key for a scope.
    /// </summary>
    public static string? GetAudioProviderKey(string scopeKey)
    {
        return GetScope(scopeKey)?.AudioProviderKey;
    }

    /// <summary>
    /// Get video provider key for a scope.
    /// </summary>
    public static string? GetVideoProviderKey(string scopeKey)
    {
        return GetScope(scopeKey)?.VideoProviderKey;
    }

    /// <summary>
    /// Add or update a subscription scope.
    /// </summary>
    private static void Add(string scopeKey, SubscriptionScope scope)
    {
        _scopes[scopeKey] = scope;
    }

    /// <summary>
    /// Remove a subscription scope.
    /// </summary>
    public static bool Remove(string scopeKey)
    {
        return _scopes.Remove(scopeKey);
    }

    /// <summary>
    /// Get all registered scopes.
    /// </summary>
    public static IReadOnlyDictionary<string, SubscriptionScope> GetAllScopes()
    {
        return _scopes.AsReadOnly();
    }

    /// <summary>
    /// Clear all scopes.
    /// </summary>
    public static void Clear()
    {
        _scopes.Clear();
    }
}
