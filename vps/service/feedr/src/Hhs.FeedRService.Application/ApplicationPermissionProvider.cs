using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Helper.Consts.Permissions;

namespace Hhs.FeedRService.Application;

public sealed class ApplicationPermissionProvider : IServicePermissionProvider
{
    public Task<List<string>> GetPermissionKeysAsync()
    {
        List<string> servicePermissionKeys = [];

        servicePermissionKeys.AddRange(FeedRAdManagerServicePermissions.GetAll());
        servicePermissionKeys.AddRange(FeedRAdManagerOperationPermissions.GetAll());

        return Task.FromResult(servicePermissionKeys);
    }

    public Task<List<string>> GetPermissionConstraintKeysAsync() => Task.FromResult(FeedRAdManagerConstraintPermissions.GetAll().ToList());
}