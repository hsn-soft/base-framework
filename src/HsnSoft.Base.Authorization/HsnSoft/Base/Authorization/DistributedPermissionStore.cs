using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Authorization;

/// <summary>
/// The grants in the distributed cache of the system (Redis, through <see cref="IDistributedCache"/>: the framework asks for no provider), one entry for each subject in a scope, as JSON. A service keeps what it read in memory for
/// <see cref="PermissionOptions.CacheSeconds"/>, so a check costs no round trip to the cache. The entries do not expire: the service that manages the grants writes them again when they change, and can write them all again when the cache was lost.
/// </summary>
public sealed class DistributedPermissionStore(IDistributedCache distributed, IMemoryCache memory, IOptions<PermissionOptions> options) : IPermissionStore
{
    public async Task<PermissionGrants> GetAsync(string? scope, PermissionSubject subject, CancellationToken cancellationToken = default)
    {
        string key = KeyOf(scope, subject);
        if (memory.TryGetValue(key, out PermissionGrants? known) && known is not null)
        {
            return known;
        }

        byte[]? stored = await distributed.GetAsync(key, cancellationToken).ConfigureAwait(false);
        PermissionGrants grants = stored is null
            ? PermissionGrants.None
            : new PermissionGrants(JsonSerializer.Deserialize<Dictionary<string, string?>>(stored) ?? []);
        int seconds = options.Value.CacheSeconds;
        if (seconds > 0)
        {
            memory.Set(key, grants, TimeSpan.FromSeconds(seconds));
        }

        return grants;
    }

    public async Task SetAsync(string? scope, PermissionSubject subject, PermissionGrants grants, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grants);

        string key = KeyOf(scope, subject);
        await distributed.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(grants.Entries), cancellationToken).ConfigureAwait(false);
        memory.Remove(key);
    }

    public async Task RemoveAsync(string? scope, PermissionSubject subject, CancellationToken cancellationToken = default)
    {
        string key = KeyOf(scope, subject);
        await distributed.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        memory.Remove(key);
    }

    /// <summary>The key of an entry: <c>{prefix}:{scope}:{provider}:{key}</c>, in small letters (a role is the same role whatever its case); no scope is a dash.</summary>
    private string KeyOf(string? scope, PermissionSubject subject)
        => $"{options.Value.KeyPrefix}:{(string.IsNullOrWhiteSpace(scope) ? "-" : scope)}:{subject.Provider}:{subject.Key}".ToLowerInvariant();
}
