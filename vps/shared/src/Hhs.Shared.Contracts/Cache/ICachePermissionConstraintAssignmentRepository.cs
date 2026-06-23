namespace Hhs.Shared.Contracts.Cache;

public interface ICachePermissionConstraintAssignmentRepository
{
    Task<List<CachePermissionConstraintAssignment>> GetPermissionsAsync(List<string> permissionKeys);

    Task<List<CachePermissionConstraintAssignment>> GetSessionPermissionsAsync(string clientKey = null, string[] roleKeys = null, string userKey = null);

    Task<bool> SetPermissionsAsync(List<CachePermissionConstraintAssignment> permissionConstraintAssignments);

    Task<bool> ClearPermissionsAsync();
}