using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Permissions;

namespace Hhs.EventManagerService.Application.Infrastructure;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync()
    {
        List<string> servicePermissionKeys = [];

        servicePermissionKeys.AddRange(EventManagerServicePermissions.GetAll());
        servicePermissionKeys.AddRange(EventManagerOperationPermissions.GetAll());

        return Task.FromResult(servicePermissionKeys);
    }

    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult(EventManagerConstraintPermissions.GetAll().ToList());
}