namespace Hhs.Shared.Contracts.Cache;

public interface IServicePermissionProvider
{
    Task<List<string>> GetServicePermissionKeysAsync();
}