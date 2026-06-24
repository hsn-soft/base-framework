using JetBrains.Annotations;

namespace Hhs.Shared.Helper.Configuration;

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
    /// Initialize with 6 test scenarios covering all meaningful combinations of providers.
    ///
    /// Provider Topology:
    /// - Outline: 2 variants (fast: 5s, queue: 10s)
    /// - Audio: 2 variants (quick: 3s, hq: 20s)
    /// - Video: 2 variants (queue-external: requires audio, queue-internal: creates own audio)
    ///
    /// Total Combinations: 2 outline × 2 audio × 1 video-external + 2 outline × 0 audio × 1 video-internal = 6
    /// </summary>
    public static void Initialize()
    {
        _scopes.Clear();

        // ========================================================================
        // GROUP 1: EXTERNAL VIDEO (requires external audio input)
        // Scenarios 001-004: All combinations of outline × audio providers
        // ========================================================================
        // Timing expectations: outline + audio + video polling cycles

        // Scenario 001: Fast outline + Quick audio + External video (polling)
        // Expected time: ~38s (5s outline + 3s audio + 30s video)
        Add("scenario-001", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: "audio-quick",
            videoProviderKey: "video-queue-external"
        ));

        // Scenario 002: Fast outline + HQ audio (polling) + External video (polling)
        // Expected time: ~53s (5s outline + 20s audio + 30s video)
        Add("scenario-002", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: "audio-hq",
            videoProviderKey: "video-queue-external"
        ));

        // Scenario 003: Queue outline (polling) + Quick audio + External video (polling)
        // Expected time: ~43s (10s outline + 3s audio + 30s video)
        Add("scenario-003", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: "audio-quick",
            videoProviderKey: "video-queue-external"
        ));

        // Scenario 004: Queue outline (polling) + HQ audio (polling) + External video (polling)
        // Expected time: ~58s (10s outline + 20s audio + 30s video)
        Add("scenario-004", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: "audio-hq",
            videoProviderKey: "video-queue-external"
        ));

        // ========================================================================
        // GROUP 2: INTERNAL VIDEO (creates audio internally, no external audio needed)
        // Scenarios 005-006: Outline provider combinations with internal video
        // Note: audioProviderKey is null because internal video generates audio
        // ========================================================================

        // Scenario 005: Fast outline + Internal video (creates own audio via polling)
        // Expected time: ~35s (5s outline + 30s video with internal audio)
        Add("scenario-005", new SubscriptionScope(
            outlineProviderKey: "outline-fast",
            audioProviderKey: null,
            videoProviderKey: "video-queue-internal"
        ));

        // Scenario 006: Queue outline (polling) + Internal video (creates own audio via polling)
        // Expected time: ~40s (10s outline + 30s video with internal audio)
        Add("scenario-006", new SubscriptionScope(
            outlineProviderKey: "outline-queue",
            audioProviderKey: null,
            videoProviderKey: "video-queue-internal"
        ));
    }

    /// <summary>
    /// Get provider configuration for a specific scope.
    /// </summary>
    [CanBeNull]
    public static SubscriptionScope GetScope(string scopeKey)
    {
        return _scopes.TryGetValue(scopeKey, out var scope) ? scope : null;
    }

    /// <summary>
    /// Get outline provider key for a scope.
    /// </summary>
    [CanBeNull]
    public static string GetOutlineProviderKey(string scopeKey)
    {
        return GetScope(scopeKey)?.OutlineProviderKey;
    }

    /// <summary>
    /// Get audio provider key for a scope.
    /// </summary>
    [CanBeNull]
    public static string GetAudioProviderKey(string scopeKey)
    {
        return GetScope(scopeKey)?.AudioProviderKey;
    }

    /// <summary>
    /// Get video provider key for a scope.
    /// </summary>
    [CanBeNull]
    public static string GetVideoProviderKey(string scopeKey)
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
