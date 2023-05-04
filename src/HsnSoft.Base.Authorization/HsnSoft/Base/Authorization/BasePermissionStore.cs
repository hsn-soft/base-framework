using System;
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
        bool validationResult = false;

        if (!string.IsNullOrWhiteSpace(providerName) && !string.IsNullOrWhiteSpace(providerKey) && !string.IsNullOrWhiteSpace(name))
        {
            validationResult = _permissions.Any(x =>
                x.ProviderName.Equals(providerName)
                && x.ProviderKey.Equals(providerKey)
                && x.Name.Equals(name));
        }

        return Task.FromResult(validationResult);
    }

    public void SetAllPermissions(IEnumerable<BasePermissionStoreItem> permissions)
    {
        _permissions = permissions ?? new List<BasePermissionStoreItem>();
    }

    public IEnumerable<BasePermissionStoreItem> GetAllPermissions()
    {
        return _permissions;
    }
}

public class BasePermissionStoreItem
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; }

    public string ProviderName { get; set; }

    public string ProviderKey { get; set; }
}