using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HsnSoft.Base.Domain.Repositories;

public class RedisRepository<T> : IRedisRepository<T> where T : class, new()
{
    private readonly IBaseLogger _logger;
    private readonly IDatabase _database;

    public RedisRepository(IBaseLogger logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _database = redis.GetDatabase();
    }

    public async Task<T> GetDataAsync(string dataKey)
    {
        if (string.IsNullOrWhiteSpace(dataKey)) return default;

        var data = await _database.StringGetAsync(new RedisKey(dataKey));

        T result;
        try
        {
            result = data.IsNullOrEmpty ? default : JsonConvert.DeserializeObject<T>(data);
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            result = default;
        }

        return result;
    }

    public async Task<bool> SetDataAsync(string dataKey, T dataValue, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(dataKey) || dataValue == null) return false;

        bool result;
        try
        {
            result = await _database.StringSetAsync(new RedisKey(dataKey), new RedisValue(JsonConvert.SerializeObject(dataValue)), expiry);
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            result = false;
        }

        return result;
    }

    public async Task<bool> RemoveDataAsync(string dataKey)
    {
        if (string.IsNullOrWhiteSpace(dataKey)) return false;

        bool result;
        try
        {
            result = await _database.KeyDeleteAsync(new RedisKey(dataKey));
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            result = false;
        }

        return result;
    }

    public async Task<IEnumerable<T>> GetDataListAsync(string listKey)
    {
        if (string.IsNullOrWhiteSpace(listKey)) return Enumerable.Empty<T>();

        try
        {
            var values = await _database.ListRangeAsync(listKey);
            if (values.Length == 0) return Enumerable.Empty<T>();

            var dataList = values.Select(value => JsonConvert.DeserializeObject<T>(value));

            return dataList;
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            return Enumerable.Empty<T>();
        }
    }

    public async Task<bool> SetDataListAsync(string listKey, T dataValue)
    {
        if (string.IsNullOrWhiteSpace(listKey) || dataValue == null) return false;

        try
        {
            var data = JsonConvert.SerializeObject(dataValue);
            await _database.ListRightPushAsync(listKey, data);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveDataFromListAsync(string listKey, T dataValue)
    {
        if (string.IsNullOrWhiteSpace(listKey) || dataValue == null) return false;

        try
        {
            var data = JsonConvert.SerializeObject(dataValue);
            await _database.ListRemoveAsync(listKey, data);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            return false;
        }
    }

    public async Task<T> ListLeftPopAsync(string listKey)
    {
        if (string.IsNullOrWhiteSpace(listKey)) return null;

        try
        {
            var dataValue = await _database.ListLeftPopAsync(listKey);
            var data = JsonConvert.DeserializeObject<T>(dataValue);
            return data;
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            return null;
        }
    }

    public async Task<int> SetAndGetIncrementalId(string dataKey)
    {
        if (string.IsNullOrWhiteSpace(dataKey)) return -1;

        try
        {
            var incrementalKey = $"{dataKey}:_index";

            return (int)_database.StringIncrement(new RedisKey(incrementalKey), 1);
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
        }

        return -1;
    }

    public async Task<T> GetIncrementalDataAsync(string dataKey, int index)
        => index >= 1 ? await GetDataAsync($"{dataKey}:{index}") : null;

    public async Task<bool> SetIncrementalDataAsync(string dataKey, int index, T dataValue, TimeSpan? expiry = null)
        => index >= 1 && await SetDataAsync($"{dataKey}:{index}", dataValue, expiry);

    public async Task<bool> RemoveIncrementalDataAsync(string dataKey, int index)
        => index >= 1 && await RemoveDataAsync($"{dataKey}:{index}");
}