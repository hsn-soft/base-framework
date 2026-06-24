using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Consts.Permissions;

namespace Hhs.TextNormalizerService.Application.Infrastructure;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync()
    {
        List<string> servicePermissionKeys = [];

        servicePermissionKeys.AddRange(TextNormalizerServicePermissions.GetAll());
        servicePermissionKeys.AddRange(TextNormalizerOperationPermissions.GetAll());

        return Task.FromResult(servicePermissionKeys);
    }

    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult(TextNormalizerConstraintPermissions.GetAll().ToList());
}