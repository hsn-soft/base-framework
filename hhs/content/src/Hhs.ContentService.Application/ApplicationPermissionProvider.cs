using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Contracts.Cache.ServicePermissions;

namespace Hhs.ContentService.Application;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetServicePermissionKeysAsync() => Task.FromResult(ContentServicePermissions.GetAll().ToList());
}