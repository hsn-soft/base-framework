namespace Hhs.Shared.Contracts.Cache;

public interface IServicePermissionProvider
{
    Task<List<string>> GetPermissionKeysAsync();
    Task<List<string>> GetPermissionConstraintKeysAsync();
}