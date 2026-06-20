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
    private static readonly Dictionary<Guid, SubscriptionScope> _scopes = [];

    /// <summary>
    /// Initialize with test/default scopes. Replace with database initialization.
    /// </summary>
    public static void Initialize()
    {
        // TODO: Load from database instead of hardcoding
        _scopes.Clear();

        // Example scopes - replace with actual database data
        Add(new SubscriptionScope(
            scopeKey: Guid.Parse("4fe789ab-0652-4e7b-bd35-07019058081d"),
            outlineProviderKey: "outline-fast",
            audioProviderKey: "audio-quick",
            videoProviderKey: "video-fast-external"
        ));

        Add(new SubscriptionScope(
            scopeKey: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            outlineProviderKey: "openai",
            audioProviderKey: "audio-hq",
            videoProviderKey: "video-queue-external"
        ));
    }

    /// <summary>
    /// Get provider configuration for a specific scope.
    /// </summary>
    public static SubscriptionScope? GetScope(Guid scopeKey)
    {
        return _scopes.TryGetValue(scopeKey, out var scope) ? scope : null;
    }

    /// <summary>
    /// Get outline provider key for a scope.
    /// </summary>
    public static string? GetOutlineProviderKey(Guid scopeKey)
    {
        return GetScope(scopeKey)?.OutlineProviderKey;
    }

    /// <summary>
    /// Get audio provider key for a scope.
    /// </summary>
    public static string? GetAudioProviderKey(Guid scopeKey)
    {
        return GetScope(scopeKey)?.AudioProviderKey;
    }

    /// <summary>
    /// Get video provider key for a scope.
    /// </summary>
    public static string? GetVideoProviderKey(Guid scopeKey)
    {
        return GetScope(scopeKey)?.VideoProviderKey;
    }

    /// <summary>
    /// Add or update a subscription scope.
    /// </summary>
    public static void Add(SubscriptionScope scope)
    {
        _scopes[scope.ScopeKey] = scope;
    }

    /// <summary>
    /// Remove a subscription scope.
    /// </summary>
    public static bool Remove(Guid scopeKey)
    {
        return _scopes.Remove(scopeKey);
    }

    /// <summary>
    /// Get all registered scopes.
    /// </summary>
    public static IReadOnlyDictionary<Guid, SubscriptionScope> GetAllScopes()
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
