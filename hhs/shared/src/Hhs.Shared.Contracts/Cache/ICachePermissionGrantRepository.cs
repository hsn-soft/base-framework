namespace Hhs.Shared.Contracts.Cache;

public interface ICachePermissionGrantRepository
{
    Task<List<CachePermissionGrant>> GetServicePermissionsAsync(List<string> permissionKeys);

    Task<List<CachePermissionGrant>> GetSessionPermissionsAsync(string clientKey = null, string[] roleKeys = null, string userKey = null);

    Task<bool> SetPermissionsAsync(List<CachePermissionGrant> permissionGrants);

    Task<bool> ClearPermissionsAsync();

    List<string> GetUsers();
}