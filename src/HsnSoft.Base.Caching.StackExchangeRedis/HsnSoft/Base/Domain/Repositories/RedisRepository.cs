using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Logging.Abstracts;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HsnSoft.Base.Domain.Repositories;

public class RedisRepository<T> : IRedisRepository<T> where T : class, new()
{
    private readonly IBaseLogger _logger;
    protected readonly IDatabase Database;

    protected RedisRepository(IBaseLogger logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        Database = redis.GetDatabase();
    }

    public async Task<T> GetDataAsync(string dataKey)
    {
        if (string.IsNullOrWhiteSpace(dataKey)) return null;

        var data = await Database.StringGetAsync(new RedisKey(dataKey));

        T result;
        try
        {
            result = data.IsNullOrEmpty ? null : JsonConvert.DeserializeObject<T>(data);
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            result = null;
        }

        return result;
    }

    public async Task<bool> SetDataAsync(string dataKey, T dataValue, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(dataKey) || dataValue == null) return false;

        bool result;
        try
        {
            result = await Database.StringSetAsync(new RedisKey(dataKey), new RedisValue(JsonConvert.SerializeObject(dataValue)), expiry);
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
            result = await Database.KeyDeleteAsync(new RedisKey(dataKey));
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
        if (string.IsNullOrWhiteSpace(listKey)) return [];

        try
        {
            var values = await Database.ListRangeAsync(listKey);
            if (values.Length == 0) return [];

            var dataList = values.Select(value => JsonConvert.DeserializeObject<T>(value));

            return dataList;
        }
        catch (Exception e)
        {
            _logger.LogError($"Redis Error: {e.Message}");
            return [];
        }
    }

    public async Task<bool> SetDataListAsync(string listKey, T dataValue)
    {
        if (string.IsNullOrWhiteSpace(listKey) || dataValue == null) return false;

        try
        {
            string data = JsonConvert.SerializeObject(dataValue);
            await Database.ListRightPushAsync(listKey, data);
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
            string data = JsonConvert.SerializeObject(dataValue);
            await Database.ListRemoveAsync(listKey, data);
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
            var dataValue = await Database.ListLeftPopAsync(listKey);
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
            string incrementalKey = $"{dataKey}:_index";

            return (int)Database.StringIncrement(new RedisKey(incrementalKey));
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