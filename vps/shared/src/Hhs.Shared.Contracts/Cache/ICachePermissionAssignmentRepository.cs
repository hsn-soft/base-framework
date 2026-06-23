namespace Hhs.Shared.Contracts.Cache;

public interface ICachePermissionAssignmentRepository
{
    Task<List<CachePermissionAssignment>> GetPermissionsAsync(List<string> permissionKeys);

    Task<List<CachePermissionAssignment>> GetSessionPermissionsAsync(string clientKey = null, string[] roleKeys = null, string userKey = null);

    Task<bool> SetPermissionsAsync(List<CachePermissionAssignment> permissionAssignments);

    Task<bool> ClearPermissionsAsync();
}