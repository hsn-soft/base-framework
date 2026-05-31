using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public class RolePermissionValueProvider(IPermissionStore permissionStore) : PermissionValueProvider(permissionStore)
{
    public override string Name => PermissionProviders.Role;

    public override async Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        string[] roles = context.Principal?.FindAll(BaseClaimTypes.Role).Select(c => c.Value).ToArray();

        if (roles == null || roles.Length == 0)
        {
            roles = context.Principal?.FindAll("role").Select(c => c.Value).ToArray();
        }

        if (roles == null || roles.Length == 0)
        {
            string clientId = context.Principal?.FindFirst(BaseClaimTypes.ClientId)?.Value;
            if (clientId == null)
            {
                return PermissionGrantResult.Undefined;
            }

            return await PermissionStore.IsGrantedAsync(context.Permission, PermissionProviders.Client, clientId)
                ? PermissionGrantResult.Granted
                : PermissionGrantResult.Undefined;
        }

        foreach (string role in roles.Distinct())
        {
            if (await PermissionStore.IsGrantedAsync(context.Permission, Name, role))
            {
                return PermissionGrantResult.Granted;
            }
        }

        return PermissionGrantResult.Undefined;
    }
}