using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Consts;
using HsnSoft.Base.Logging.Abstracts;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hhs.Shared.Hosting.Microservices.Cache;

public class CachePermissionGrantRepository : ICachePermissionGrantRepository
{
    private const string PermissionGrantStoreKey = $"{NameConsts.SolutionName}-permission-grant-store";
    private readonly IAppConsoleLogger _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public CachePermissionGrantRepository(IAppConsoleLogger logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
        _database = redis.GetDatabase();
    }

    public async Task<List<CachePermissionGrant>> GetServicePermissionsAsync(List<string> permissionKeys) => permissionKeys is not { Count: > 0 }
        ? []
        : (await GetPermissionsAsync()).Where(x => permissionKeys.Contains(x.Name)).ToList();

    public async Task<List<CachePermissionGrant>> GetSessionPermissionsAsync(string clientKey = null, string[] roleKeys = null, string userKey = null)
    {
        var result = new List<CachePermissionGrant>();
        var cachedPermissions = await GetPermissionsAsync();
        if (cachedPermissions == null) return result;

        if (roleKeys is { Length: > 0 })
        {
            result.AddRange(cachedPermissions.Where(e => e.ProviderName.Equals("R") && roleKeys.Contains(e.ProviderKey)).ToList());
        }
        else if (!string.IsNullOrWhiteSpace(clientKey))
        {
            result.AddRange(cachedPermissions.Where(e => e.ProviderName.Equals("C") && e.ProviderKey.Equals(clientKey)).ToList());
        }

        if (!string.IsNullOrWhiteSpace(userKey))
        {
            result.AddRange(cachedPermissions.Where(e => e.ProviderName.Equals("U") && e.ProviderKey.Equals(userKey)).ToList());
        }

        return result;
    }

    public async Task<bool> SetPermissionsAsync(List<CachePermissionGrant> permissionGrants)
    {
        permissionGrants ??= [];
        return await _database.StringSetAsync(new RedisKey(PermissionGrantStoreKey), new RedisValue(JsonConvert.SerializeObject(permissionGrants)));
    }

    public async Task<bool> ClearPermissionsAsync()
    {
        return await _database.KeyDeleteAsync(new RedisKey(PermissionGrantStoreKey));
    }

    public List<string> GetUsers()
    {
        _logger.LogInformation("Get User Info");
        var server = GetServer();
        var data = server.Keys();
        return data.Select(k => k.ToString()).ToList();
    }

    private async Task<List<CachePermissionGrant>> GetPermissionsAsync()
    {
        var data = await _database.StringGetAsync(new RedisKey(PermissionGrantStoreKey));

        return data.IsNullOrEmpty ? [] : JsonConvert.DeserializeObject<List<CachePermissionGrant>>(data);
    }

    private IServer GetServer()
    {
        var endpoint = _redis.GetEndPoints();
        return _redis.GetServer(endpoint.First());
    }
}