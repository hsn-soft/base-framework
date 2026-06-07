using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Consts;
using HsnSoft.Base.Authorization.Permissions.ValueProviders;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hhs.Shared.Hosting.Microservices.Cache;

public class CachePermissionAssignmentRepository(IConnectionMultiplexer redis) : ICachePermissionAssignmentRepository
{
    private const string PermissionAssignmentStoreKey = $"{NameConsts.SolutionName}-permission-store";
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<List<CachePermissionAssignment>> GetPermissionsAsync(List<string> permissionKeys)
        => permissionKeys is not { Count: > 0 }
            ? []
            : (await GetPermissionsAsync()).Where(x => permissionKeys.Contains(x.Permission)).ToList();

    public async Task<List<CachePermissionAssignment>> GetSessionPermissionsAsync(string clientKey = null, string[] roleKeys = null, string userKey = null)
    {
        var result = new List<CachePermissionAssignment>();
        var cachedPermissions = await GetPermissionsAsync();
        if (cachedPermissions == null) return result;

        if (roleKeys is { Length: > 0 })
        {
            result.AddRange(cachedPermissions
                .Where(e => e.ProviderName.Equals(PermissionProviders.Role) && roleKeys.Contains(e.ProviderKey))
                .ToList()
            );
        }
        else if (!string.IsNullOrWhiteSpace(clientKey))
        {
            result.AddRange(cachedPermissions
                .Where(e => e.ProviderName.Equals(PermissionProviders.Client) && e.ProviderKey.Equals(clientKey))
                .ToList()
            );
        }

        if (!string.IsNullOrWhiteSpace(userKey))
        {
            result.AddRange(cachedPermissions
                .Where(e => e.ProviderName.Equals(PermissionProviders.User) && e.ProviderKey.Equals(userKey))
                .ToList()
            );
        }

        return result;
    }

    public async Task<bool> SetPermissionsAsync(List<CachePermissionAssignment> permissionAssignments)
        => await _database.StringSetAsync(new RedisKey(PermissionAssignmentStoreKey), new RedisValue(JsonConvert.SerializeObject(permissionAssignments ?? [])));

    public async Task<bool> ClearPermissionsAsync()
        => await _database.KeyDeleteAsync(new RedisKey(PermissionAssignmentStoreKey));


    private async Task<List<CachePermissionAssignment>> GetPermissionsAsync()
    {
        var data = await _database.StringGetAsync(new RedisKey(PermissionAssignmentStoreKey));

        return data.IsNullOrEmpty ? [] : JsonConvert.DeserializeObject<List<CachePermissionAssignment>>(data);
    }
}