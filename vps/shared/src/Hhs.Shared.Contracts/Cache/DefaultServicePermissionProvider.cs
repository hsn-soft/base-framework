namespace Hhs.Shared.Contracts.Cache;

public sealed class DefaultServicePermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync() => Task.FromResult<List<string>>([]);
    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult<List<string>>([]);
}