using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Consts.Permissions;

namespace Hhs.AdministrationService.Application.Infrastructure;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync()
    {
        List<string> servicePermissionKeys = [];

        servicePermissionKeys.AddRange(AdministrationServicePermissions.GetAll());
        servicePermissionKeys.AddRange(AdministrationOperationPermissions.GetAll());

        return Task.FromResult(servicePermissionKeys);
    }

    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult(AdministrationConstraintPermissions.GetAll().ToList());
}