using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Permissions;

namespace Hhs.ContentService.Application.Infrastructure;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync()
    {
        List<string> servicePermissionKeys = [];

        servicePermissionKeys.AddRange(ContentServicePermissions.GetAll());
        servicePermissionKeys.AddRange(ContentOperationPermissions.GetAll());

        return Task.FromResult(servicePermissionKeys);
    }

    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult(ContentConstraintPermissions.GetAll().ToList());
}