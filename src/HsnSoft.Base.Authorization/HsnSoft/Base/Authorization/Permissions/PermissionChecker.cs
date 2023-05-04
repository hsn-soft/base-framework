using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions;

public class PermissionChecker : IPermissionChecker, ITransientDependency
{
    private readonly List<IPermissionValueProvider> _providers;

    public PermissionChecker(
        ICurrentPrincipalAccessor principalAccessor,
        ICurrentTenant currentTenant,
        IPermissionStore permissionStore)
    {
        PrincipalAccessor = principalAccessor;
        PermissionStore = permissionStore;

        _providers = new List<IPermissionValueProvider> { new ClientPermissionValueProvider(PermissionStore, currentTenant), new RolePermissionValueProvider(PermissionStore), new UserPermissionValueProvider(PermissionStore), };
    }

    protected ICurrentPrincipalAccessor PrincipalAccessor { get; }
    protected IPermissionStore PermissionStore { get; }

    public virtual async Task<bool> IsGrantedAsync(string name)
    {
        return await IsGrantedAsync(PrincipalAccessor.Principal, name);
    }

    public virtual async Task<bool> IsGrantedAsync(ClaimsPrincipal claimsPrincipal, string name)
    {
        Check.NotNull(name, nameof(name));

        var isGranted = false;
        var context = new PermissionValueCheckContext(name, claimsPrincipal);
        foreach (var provider in _providers)
        {
            var result = await provider.CheckAsync(context);

            if (result == PermissionGrantResult.Granted)
            {
                isGranted = true;
            }
            else if (result == PermissionGrantResult.Prohibited)
            {
                return false;
            }
        }

        return isGranted;
    }
}