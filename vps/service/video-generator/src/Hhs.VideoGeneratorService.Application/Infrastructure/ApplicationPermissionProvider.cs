using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Consts.Permissions;

namespace Hhs.VideoGeneratorService.Application.Infrastructure;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync()
    {
        List<string> servicePermissionKeys = [];

        servicePermissionKeys.AddRange(VideoGeneratorServicePermissions.GetAll());
        servicePermissionKeys.AddRange(VideoGeneratorOperationPermissions.GetAll());

        return Task.FromResult(servicePermissionKeys);
    }

    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult(VideoGeneratorConstraintPermissions.GetAll().ToList());
}