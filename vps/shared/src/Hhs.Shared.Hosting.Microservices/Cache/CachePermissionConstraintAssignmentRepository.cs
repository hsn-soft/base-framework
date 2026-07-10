using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Constants;
using HsnSoft.Base.Authorization.Permissions.ValueProviders;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Hhs.Shared.Hosting.Microservices.Cache;

public class CachePermissionConstraintAssignmentRepository(IConnectionMultiplexer redis) : ICachePermissionConstraintAssignmentRepository
{
    private const string PermissionConstraintAssignmentStoreKey = $"{NameConsts.SolutionName}-permission-constraint-store";
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<List<CachePermissionConstraintAssignment>> GetPermissionsAsync(List<string> permissionKeys)
        => permissionKeys is not { Count: > 0 }
            ? []
            : (await GetPermissionsAsync()).Where(x => permissionKeys.Contains(x.Constraint)).ToList();

    public async Task<List<CachePermissionConstraintAssignment>> GetSessionPermissionsAsync(string clientKey = null, string[] roleKeys = null, string userKey = null)
    {
        var result = new List<CachePermissionConstraintAssignment>();
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

    public async Task<bool> SetPermissionsAsync(List<CachePermissionConstraintAssignment> permissionConstraintAssignments)
        => await _database.StringSetAsync(new RedisKey(PermissionConstraintAssignmentStoreKey), new RedisValue(JsonConvert.SerializeObject(permissionConstraintAssignments ?? [])));

    public async Task<bool> ClearPermissionsAsync()
        => await _database.KeyDeleteAsync(new RedisKey(PermissionConstraintAssignmentStoreKey));


    private async Task<List<CachePermissionConstraintAssignment>> GetPermissionsAsync()
    {
        var data = await _database.StringGetAsync(new RedisKey(PermissionConstraintAssignmentStoreKey));

        return data.IsNullOrEmpty ? [] : JsonConvert.DeserializeObject<List<CachePermissionConstraintAssignment>>(data);
    }
}