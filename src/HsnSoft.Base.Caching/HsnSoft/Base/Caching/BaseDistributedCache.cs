using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.ExceptionHandling;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace HsnSoft.Base.Caching;

public class BaseDistributedCache<TCacheItem> :
    BaseDistributedCache<TCacheItem, string>,
    IBaseDistributedCache<TCacheItem>
    where TCacheItem : class
{
    public BaseDistributedCache(
        IOptions<BaseDistributedCacheOptions> distributedCacheOption,
        IDistributedCache cache,
        ICancellationTokenProvider cancellationTokenProvider,
        IDistributedCacheSerializer serializer,
        IDistributedCacheKeyNormalizer keyNormalizer,
        IServiceScopeFactory serviceScopeFactory) : base(
        distributedCacheOption: distributedCacheOption,
        cache: cache,
        cancellationTokenProvider: cancellationTokenProvider,
        serializer: serializer,
        keyNormalizer: keyNormalizer,
        serviceScopeFactory: serviceScopeFactory)
    {
    }
}

public class BaseDistributedCache<TCacheItem, TCacheKey> : IBaseDistributedCache<TCacheItem, TCacheKey>
    where TCacheItem : class
{
    public const string UowCacheName = "AbpDistributedCache";

    public ILogger<BaseDistributedCache<TCacheItem, TCacheKey>> Logger { get; set; }

    protected string CacheName { get; set; }

    protected bool IgnoreMultiTenancy { get; set; }

    protected IDistributedCache Cache { get; }

    protected ICancellationTokenProvider CancellationTokenProvider { get; }

    protected IDistributedCacheSerializer Serializer { get; }

    protected IDistributedCacheKeyNormalizer KeyNormalizer { get; }

    protected IServiceScopeFactory ServiceScopeFactory { get; }

    protected SemaphoreSlim SyncSemaphore { get; }

    protected DistributedCacheEntryOptions DefaultCacheOptions;

    private readonly BaseDistributedCacheOptions _distributedCacheOption;

    public BaseDistributedCache(
        IOptions<BaseDistributedCacheOptions> distributedCacheOption,
        IDistributedCache cache,
        ICancellationTokenProvider cancellationTokenProvider,
        IDistributedCacheSerializer serializer,
        IDistributedCacheKeyNormalizer keyNormalizer,
        IServiceScopeFactory serviceScopeFactory)
    {
        _distributedCacheOption = distributedCacheOption.Value;
        Cache = cache;
        CancellationTokenProvider = cancellationTokenProvider;
        Logger = NullLogger<BaseDistributedCache<TCacheItem, TCacheKey>>.Instance;
        Serializer = serializer;
        KeyNormalizer = keyNormalizer;
        ServiceScopeFactory = serviceScopeFactory;

        SyncSemaphore = new SemaphoreSlim(1, 1);

        SetDefaultOptions();
    }

    protected virtual string NormalizeKey(TCacheKey key)
    {
        return KeyNormalizer.NormalizeKey(
            new DistributedCacheKeyNormalizeArgs(
                key.ToString(),
                CacheName,
                IgnoreMultiTenancy
            )
        );
    }

    protected virtual DistributedCacheEntryOptions GetDefaultCacheEntryOptions()
    {
        foreach (var configure in _distributedCacheOption.CacheConfigurators)
        {
            var options = configure.Invoke(CacheName);
            if (options != null)
            {
                return options;
            }
        }

        return _distributedCacheOption.GlobalCacheEntryOptions;
    }

    protected virtual void SetDefaultOptions()
    {
        CacheName = CacheNameAttribute.GetCacheName(typeof(TCacheItem));

        //IgnoreMultiTenancy
        IgnoreMultiTenancy = typeof(TCacheItem).IsDefined(typeof(IgnoreMultiTenancyAttribute), true);

        //Configure default cache entry options
        DefaultCacheOptions = GetDefaultCacheEntryOptions();
    }

    public virtual TCacheItem Get(TCacheKey key, bool? hideErrors = null)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        byte[] cachedBytes;

        try
        {
            cachedBytes = Cache.Get(NormalizeKey(key));
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return null;
            }

            throw;
        }

        return ToCacheItem(cachedBytes);
    }

    public virtual KeyValuePair<TCacheKey, TCacheItem>[] GetMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null)
    {
        var keyArray = keys.ToArray();

        var cacheSupportsMultipleItems = Cache as ICacheSupportsMultipleItems;
        if (cacheSupportsMultipleItems == null)
        {
            return GetManyFallback(
                keyArray,
                hideErrors
            );
        }

        var notCachedKeys = new List<TCacheKey>();
        var cachedValues = new List<KeyValuePair<TCacheKey, TCacheItem>>();

        hideErrors ??= _distributedCacheOption.HideErrors;
        byte[][] cachedBytes;

        var readKeys = notCachedKeys.Any() ? notCachedKeys.ToArray() : keyArray;
        try
        {
            cachedBytes = cacheSupportsMultipleItems.GetMany(readKeys.Select(NormalizeKey));
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return ToCacheItemsWithDefaultValues(keyArray);
            }

            throw;
        }

        return cachedValues.Concat(ToCacheItems(cachedBytes, readKeys)).ToArray();
    }

    protected virtual KeyValuePair<TCacheKey, TCacheItem>[] GetManyFallback(TCacheKey[] keys, bool? hideErrors = null)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            return keys
                .Select(key => new KeyValuePair<TCacheKey, TCacheItem>(
                        key,
                        Get(key, false)
                    )
                ).ToArray();
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return ToCacheItemsWithDefaultValues(keys);
            }

            throw;
        }
    }

    public virtual async Task<KeyValuePair<TCacheKey, TCacheItem>[]> GetManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default)
    {
        var keyArray = keys.ToArray();

        var cacheSupportsMultipleItems = Cache as ICacheSupportsMultipleItems;
        if (cacheSupportsMultipleItems == null)
        {
            return await GetManyFallbackAsync(
                keyArray,
                hideErrors,
                token
            );
        }

        var notCachedKeys = new List<TCacheKey>();
        var cachedValues = new List<KeyValuePair<TCacheKey, TCacheItem>>();

        hideErrors ??= _distributedCacheOption.HideErrors;
        byte[][] cachedBytes;

        var readKeys = notCachedKeys.Any() ? notCachedKeys.ToArray() : keyArray;

        try
        {
            cachedBytes = await cacheSupportsMultipleItems.GetManyAsync(
                readKeys.Select(NormalizeKey),
                CancellationTokenProvider.FallbackToProvider(token)
            );
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                await HandleExceptionAsync(ex);
                return ToCacheItemsWithDefaultValues(keyArray);
            }

            throw;
        }

        return cachedValues.Concat(ToCacheItems(cachedBytes, readKeys)).ToArray();
    }

    protected virtual async Task<KeyValuePair<TCacheKey, TCacheItem>[]> GetManyFallbackAsync(TCacheKey[] keys, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            var result = new List<KeyValuePair<TCacheKey, TCacheItem>>();

            foreach (var key in keys)
            {
                result.Add(new KeyValuePair<TCacheKey, TCacheItem>(
                    key,
                    await GetAsync(key, false, token: token))
                );
            }

            return result.ToArray();
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                await HandleExceptionAsync(ex);
                return ToCacheItemsWithDefaultValues(keys);
            }

            throw;
        }
    }

    public virtual async Task<TCacheItem> GetAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        byte[] cachedBytes;

        try
        {
            cachedBytes = await Cache.GetAsync(
                NormalizeKey(key),
                CancellationTokenProvider.FallbackToProvider(token)
            );
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                await HandleExceptionAsync(ex);
                return null;
            }

            throw;
        }

        if (cachedBytes == null)
        {
            return null;
        }

        return Serializer.Deserialize<TCacheItem>(cachedBytes);
    }

    public virtual TCacheItem GetOrAdd(TCacheKey key, Func<TCacheItem> factory, Func<DistributedCacheEntryOptions> optionsFactory = null, bool? hideErrors = null)
    {
        var value = Get(key, hideErrors);
        if (value != null)
        {
            return value;
        }

        using (SyncSemaphore.Lock())
        {
            value = Get(key, hideErrors);
            if (value != null)
            {
                return value;
            }

            value = factory();

            Set(key, value, optionsFactory?.Invoke(), hideErrors);
        }

        return value;
    }

    public virtual async Task<TCacheItem> GetOrAddAsync(TCacheKey key, Func<Task<TCacheItem>> factory, Func<DistributedCacheEntryOptions> optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
    {
        token = CancellationTokenProvider.FallbackToProvider(token);
        var value = await GetAsync(key, hideErrors, token);
        if (value != null)
        {
            return value;
        }

        using (await SyncSemaphore.LockAsync(token))
        {
            value = await GetAsync(key, hideErrors, token);
            if (value != null)
            {
                return value;
            }

            value = await factory();

            await SetAsync(key, value, optionsFactory?.Invoke(), hideErrors, token);
        }

        return value;
    }

    public KeyValuePair<TCacheKey, TCacheItem>[] GetOrAddMany(IEnumerable<TCacheKey> keys, Func<IEnumerable<TCacheKey>, List<KeyValuePair<TCacheKey, TCacheItem>>> factory, Func<DistributedCacheEntryOptions> optionsFactory = null, bool? hideErrors = null)
    {
        KeyValuePair<TCacheKey, TCacheItem>[] result;
        var keyArray = keys.ToArray();

        var cacheSupportsMultipleItems = Cache as ICacheSupportsMultipleItems;
        if (cacheSupportsMultipleItems == null)
        {
            result = GetManyFallback(
                keyArray,
                hideErrors
            );
        }
        else
        {
            var notCachedKeys = new List<TCacheKey>();
            var cachedValues = new List<KeyValuePair<TCacheKey, TCacheItem>>();


            hideErrors ??= _distributedCacheOption.HideErrors;
            byte[][] cachedBytes;

            var readKeys = notCachedKeys.Any() ? notCachedKeys.ToArray() : keyArray;
            try
            {
                cachedBytes = cacheSupportsMultipleItems.GetMany(readKeys.Select(NormalizeKey));
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    HandleException(ex);
                    return ToCacheItemsWithDefaultValues(keyArray);
                }

                throw;
            }

            result = cachedValues.Concat(ToCacheItems(cachedBytes, readKeys)).ToArray();
        }

        if (result.All(x => x.Value != null))
        {
            return result;
        }

        var missingKeys = new List<TCacheKey>();
        var missingValuesIndex = new List<int>();
        for (var i = 0; i < keyArray.Length; i++)
        {
            if (result[i].Value != null)
            {
                continue;
            }

            missingKeys.Add(keyArray[i]);
            missingValuesIndex.Add(i);
        }

        var missingValues = factory.Invoke(missingKeys).ToArray();
        var valueQueue = new Queue<KeyValuePair<TCacheKey, TCacheItem>>(missingValues);

        SetMany(missingValues, optionsFactory?.Invoke(), hideErrors);

        foreach (var index in missingValuesIndex)
        {
            result[index] = valueQueue.Dequeue();
        }

        return result;
    }

    public async Task<KeyValuePair<TCacheKey, TCacheItem>[]> GetOrAddManyAsync(IEnumerable<TCacheKey> keys, Func<IEnumerable<TCacheKey>, Task<List<KeyValuePair<TCacheKey, TCacheItem>>>> factory, Func<DistributedCacheEntryOptions> optionsFactory = null, bool? hideErrors = null, CancellationToken token = default)
    {
        KeyValuePair<TCacheKey, TCacheItem>[] result;
        var keyArray = keys.ToArray();

        var cacheSupportsMultipleItems = Cache as ICacheSupportsMultipleItems;
        if (cacheSupportsMultipleItems == null)
        {
            result = await GetManyFallbackAsync(
                keyArray,
                hideErrors,
                token);
        }
        else
        {
            var notCachedKeys = new List<TCacheKey>();
            var cachedValues = new List<KeyValuePair<TCacheKey, TCacheItem>>();

            hideErrors ??= _distributedCacheOption.HideErrors;
            byte[][] cachedBytes;

            var readKeys = notCachedKeys.Any() ? notCachedKeys.ToArray() : keyArray;
            try
            {
                cachedBytes = await cacheSupportsMultipleItems.GetManyAsync(readKeys.Select(NormalizeKey), token);
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    await HandleExceptionAsync(ex);
                    return ToCacheItemsWithDefaultValues(keyArray);
                }

                throw;
            }

            result = cachedValues.Concat(ToCacheItems(cachedBytes, readKeys)).ToArray();
        }

        if (result.All(x => x.Value != null))
        {
            return result;
        }

        var missingKeys = new List<TCacheKey>();
        var missingValuesIndex = new List<int>();
        for (var i = 0; i < keyArray.Length; i++)
        {
            if (result[i].Value != null)
            {
                continue;
            }

            missingKeys.Add(keyArray[i]);
            missingValuesIndex.Add(i);
        }

        var missingValues = (await factory.Invoke(missingKeys)).ToArray();
        var valueQueue = new Queue<KeyValuePair<TCacheKey, TCacheItem>>(missingValues);

        await SetManyAsync(missingValues, optionsFactory?.Invoke(), hideErrors, token);

        foreach (var index in missingValuesIndex)
        {
            result[index] = valueQueue.Dequeue();
        }

        return result;
    }

    public virtual void Set(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions options = null, bool? hideErrors = null)
    {
        void SetRealCache()
        {
            hideErrors ??= _distributedCacheOption.HideErrors;

            try
            {
                Cache.Set(
                    NormalizeKey(key),
                    Serializer.Serialize(value),
                    options ?? DefaultCacheOptions
                );
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    HandleException(ex);
                    return;
                }

                throw;
            }
        }

        SetRealCache();
    }

    public virtual async Task SetAsync(TCacheKey key, TCacheItem value, DistributedCacheEntryOptions options = null, bool? hideErrors = null, CancellationToken token = default)
    {
        async Task SetRealCache()
        {
            hideErrors ??= _distributedCacheOption.HideErrors;

            try
            {
                await Cache.SetAsync(
                    NormalizeKey(key),
                    Serializer.Serialize(value),
                    options ?? DefaultCacheOptions,
                    CancellationTokenProvider.FallbackToProvider(token)
                );
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    await HandleExceptionAsync(ex);
                    return;
                }

                throw;
            }
        }

        await SetRealCache();
    }

    public void SetMany(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items, DistributedCacheEntryOptions options = null, bool? hideErrors = null)
    {
        var itemsArray = items.ToArray();

        var cacheSupportsMultipleItems = Cache as ICacheSupportsMultipleItems;
        if (cacheSupportsMultipleItems == null)
        {
            SetManyFallback(
                itemsArray,
                options,
                hideErrors
            );

            return;
        }

        void SetRealCache()
        {
            hideErrors ??= _distributedCacheOption.HideErrors;

            try
            {
                cacheSupportsMultipleItems.SetMany(
                    ToRawCacheItems(itemsArray),
                    options ?? DefaultCacheOptions
                );
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    HandleException(ex);
                    return;
                }

                throw;
            }
        }

        SetRealCache();
    }

    protected virtual void SetManyFallback(KeyValuePair<TCacheKey, TCacheItem>[] items, DistributedCacheEntryOptions options = null, bool? hideErrors = null)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            foreach (var item in items)
            {
                Set(
                    item.Key,
                    item.Value,
                    options,
                    false
                );
            }
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return;
            }

            throw;
        }
    }

    public virtual async Task SetManyAsync(IEnumerable<KeyValuePair<TCacheKey, TCacheItem>> items, DistributedCacheEntryOptions options = null, bool? hideErrors = null, CancellationToken token = default)
    {
        var itemsArray = items.ToArray();

        var cacheSupportsMultipleItems = Cache as ICacheSupportsMultipleItems;
        if (cacheSupportsMultipleItems == null)
        {
            await SetManyFallbackAsync(
                itemsArray,
                options,
                hideErrors,
                token
            );

            return;
        }

        async Task SetRealCache()
        {
            hideErrors ??= _distributedCacheOption.HideErrors;

            try
            {
                await cacheSupportsMultipleItems.SetManyAsync(
                    ToRawCacheItems(itemsArray),
                    options ?? DefaultCacheOptions,
                    CancellationTokenProvider.FallbackToProvider(token)
                );
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    await HandleExceptionAsync(ex);
                    return;
                }

                throw;
            }
        }

        await SetRealCache();
    }

    protected virtual async Task SetManyFallbackAsync(KeyValuePair<TCacheKey, TCacheItem>[] items, DistributedCacheEntryOptions options = null, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            foreach (var item in items)
            {
                await SetAsync(
                    item.Key,
                    item.Value,
                    options,
                    false,
                    token: token
                );
            }
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                await HandleExceptionAsync(ex);
                return;
            }

            throw;
        }
    }

    public virtual void Refresh(TCacheKey key, bool? hideErrors = null)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            Cache.Refresh(NormalizeKey(key));
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return;
            }

            throw;
        }
    }

    public virtual async Task RefreshAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            await Cache.RefreshAsync(NormalizeKey(key), CancellationTokenProvider.FallbackToProvider(token));
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                await HandleExceptionAsync(ex);
                return;
            }

            throw;
        }
    }

    public virtual void RefreshMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            if (Cache is ICacheSupportsMultipleItems cacheSupportsMultipleItems)
            {
                cacheSupportsMultipleItems.RefreshMany(keys.Select(NormalizeKey));
            }
            else
            {
                foreach (var key in keys)
                {
                    Cache.Refresh(NormalizeKey(key));
                }
            }
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                HandleException(ex);
                return;
            }

            throw;
        }
    }

    public virtual async Task RefreshManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default)
    {
        hideErrors ??= _distributedCacheOption.HideErrors;

        try
        {
            if (Cache is ICacheSupportsMultipleItems cacheSupportsMultipleItems)
            {
                await cacheSupportsMultipleItems.RefreshManyAsync(keys.Select(NormalizeKey), token);
            }
            else
            {
                foreach (var key in keys)
                {
                    await Cache.RefreshAsync(NormalizeKey(key), token);
                }
            }
        }
        catch (Exception ex)
        {
            if (hideErrors == true)
            {
                await HandleExceptionAsync(ex);
                return;
            }

            throw;
        }
    }

    public virtual void Remove(TCacheKey key, bool? hideErrors = null)
    {
        void RemoveRealCache()
        {
            hideErrors ??= _distributedCacheOption.HideErrors;

            try
            {
                Cache.Remove(NormalizeKey(key));
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    HandleException(ex);
                    return;
                }

                throw;
            }
        }

        RemoveRealCache();
    }

    public virtual async Task RemoveAsync(TCacheKey key, bool? hideErrors = null, CancellationToken token = default)
    {
        async Task RemoveRealCache()
        {
            hideErrors ??= _distributedCacheOption.HideErrors;

            try
            {
                await Cache.RemoveAsync(NormalizeKey(key), CancellationTokenProvider.FallbackToProvider(token));
            }
            catch (Exception ex)
            {
                if (hideErrors == true)
                {
                    await HandleExceptionAsync(ex);
                    return;
                }

                throw;
            }
        }

        await RemoveRealCache();
    }

    public void RemoveMany(IEnumerable<TCacheKey> keys, bool? hideErrors = null)
    {
        var keyArray = keys.ToArray();

        if (Cache is ICacheSupportsMultipleItems cacheSupportsMultipleItems)
        {
            void RemoveRealCache()
            {
                hideErrors ??= _distributedCacheOption.HideErrors;

                try
                {
                    cacheSupportsMultipleItems.RemoveMany(
                        keyArray.Select(NormalizeKey)
                    );
                }
                catch (Exception ex)
                {
                    if (hideErrors == true)
                    {
                        HandleException(ex);
                        return;
                    }

                    throw;
                }
            }

            RemoveRealCache();
        }
        else
        {
            foreach (var key in keyArray)
            {
                Remove(key, hideErrors);
            }
        }
    }

    public async Task RemoveManyAsync(IEnumerable<TCacheKey> keys, bool? hideErrors = null, CancellationToken token = default)
    {
        var keyArray = keys.ToArray();

        if (Cache is ICacheSupportsMultipleItems cacheSupportsMultipleItems)
        {
            async Task RemoveRealCache()
            {
                hideErrors ??= _distributedCacheOption.HideErrors;

                try
                {
                    await cacheSupportsMultipleItems.RemoveManyAsync(
                        keyArray.Select(NormalizeKey), token);
                }
                catch (Exception ex)
                {
                    if (hideErrors == true)
                    {
                        await HandleExceptionAsync(ex);
                        return;
                    }

                    throw;
                }
            }

            await RemoveRealCache();
        }
        else
        {
            foreach (var key in keyArray)
            {
                await RemoveAsync(key, hideErrors, token);
            }
        }
    }

    protected virtual void HandleException(Exception ex)
    {
        _ = HandleExceptionAsync(ex);
    }

    protected virtual async Task HandleExceptionAsync(Exception ex)
    {
        Logger.LogException(ex, LogLevel.Warning);

        using (var scope = ServiceScopeFactory.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IExceptionNotifier>()
                .NotifyAsync(new ExceptionNotificationContext(ex, LogLevel.Warning));
        }
    }

    protected virtual KeyValuePair<TCacheKey, TCacheItem>[] ToCacheItems(byte[][] itemBytes, TCacheKey[] itemKeys)
    {
        if (itemBytes.Length != itemKeys.Length)
        {
            throw new BaseException("count of the item bytes should be same with the count of the given keys");
        }

        var result = new List<KeyValuePair<TCacheKey, TCacheItem>>();

        for (int i = 0; i < itemKeys.Length; i++)
        {
            result.Add(
                new KeyValuePair<TCacheKey, TCacheItem>(
                    itemKeys[i],
                    ToCacheItem(itemBytes[i])
                )
            );
        }

        return result.ToArray();
    }

    [CanBeNull]
    protected virtual TCacheItem ToCacheItem([CanBeNull] byte[] bytes)
    {
        if (bytes == null)
        {
            return null;
        }

        return Serializer.Deserialize<TCacheItem>(bytes);
    }

    protected virtual KeyValuePair<string, byte[]>[] ToRawCacheItems(KeyValuePair<TCacheKey, TCacheItem>[] items)
    {
        return items
            .Select(i => new KeyValuePair<string, byte[]>(
                    NormalizeKey(i.Key),
                    Serializer.Serialize(i.Value)
                )
            ).ToArray();
    }

    private static KeyValuePair<TCacheKey, TCacheItem>[] ToCacheItemsWithDefaultValues(TCacheKey[] keys)
    {
        return keys
            .Select(key => new KeyValuePair<TCacheKey, TCacheItem>(key, default))
            .ToArray();
    }
}