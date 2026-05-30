namespace Hhs.Shared.Contracts.Cache;

public sealed class DefaultServicePermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetServicePermissionKeysAsync() => Task.FromResult<List<string>>([]);
}