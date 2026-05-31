using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.ValueProviders;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions;

public sealed class PermissionChecker(IEnumerable<IPermissionValueProvider> providers, ICurrentPrincipalAccessor principalAccessor) : IPermissionChecker
{
    private ICurrentPrincipalAccessor PrincipalAccessor { get; } = principalAccessor;

    public Task<bool> IsGrantedAsync(string name)
    {
        return IsGrantedAsync(PrincipalAccessor.Principal, name);
    }

    public async Task<bool> IsGrantedAsync(ClaimsPrincipal principal, string name)
    {
        bool granted = false;

        foreach (var provider in providers)
        {
            var result = await provider.CheckAsync(new PermissionValueCheckContext(name, principal));

            if (result == PermissionGrantResult.Prohibited)
                return false;

            if (result != PermissionGrantResult.Granted)
            {
                continue;
            }

            granted = true;
            break;

        }

        return granted;
    }
}