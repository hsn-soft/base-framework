using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Permissions;

namespace Hhs.IdentityService.Application.Infrastructure;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync()
    {
        List<string> servicePermissionKeys = [];

        servicePermissionKeys.AddRange(IdentityServicePermissions.GetAll());
        servicePermissionKeys.AddRange(IdentityOperationPermissions.GetAll());

        return Task.FromResult(servicePermissionKeys);
    }

    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult(IdentityConstraintPermissions.GetAll().ToList());
}