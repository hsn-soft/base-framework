using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.Authorization;

public class BasePermissionStore : IPermissionStore, ISingletonDependency
{
    private IEnumerable<BasePermissionStoreItem> _permissions = new List<BasePermissionStoreItem>();

    public BasePermissionStore()
    {
        Logger = NullLogger<BasePermissionStore>.Instance;
    }

    public ILogger<BasePermissionStore> Logger { get; set; }

    public Task<bool> IsGrantedAsync(string name, string providerName, string providerKey)
    {
        var validationResult = false;

        if (!string.IsNullOrWhiteSpace(providerName) && !string.IsNullOrWhiteSpace(providerKey) && !string.IsNullOrWhiteSpace(name))
        {
            validationResult = _permissions.Any(x =>
                x.ProviderName.Equals(providerName)
                && x.ProviderKey.Equals(providerKey)
                && x.Name.Equals(name));
        }

        return Task.FromResult(validationResult);
    }

    public Task SetAllPermissions(IEnumerable<BasePermissionStoreItem> permissions)
    {
        _permissions = permissions ?? new List<BasePermissionStoreItem>();
        return Task.CompletedTask;
    }

    public Task<IEnumerable<BasePermissionStoreItem>> GetAllPermissions()
    {
        return Task.FromResult(_permissions);
    }
}