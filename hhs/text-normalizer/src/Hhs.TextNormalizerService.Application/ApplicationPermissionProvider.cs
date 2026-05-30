using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Contracts.Cache.ServicePermissions;

namespace Hhs.TextNormalizerService.Application;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetServicePermissionKeysAsync() => Task.FromResult(TextNormalizerServicePermissions.GetAll().ToList());
}