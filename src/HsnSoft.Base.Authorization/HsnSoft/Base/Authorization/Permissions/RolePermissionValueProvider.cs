using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions;

public class RolePermissionValueProvider : PermissionValueProvider
{
    public const string ProviderName = "R";

    public override string Name => ProviderName;

    public RolePermissionValueProvider(IPermissionStore permissionStore) : base(permissionStore)
    {
    }

    public override async Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        var roles = context.Principal?.FindAll(BaseClaimTypes.Role).Select(c => c.Value).ToArray();

        if (roles == null || !roles.Any())
        {
            roles = context.Principal?.FindAll("role").Select(c => c.Value).ToArray();
        }

        if (roles == null || !roles.Any())
        {
            return PermissionGrantResult.Undefined;
        }

        foreach (var role in roles.Distinct())
        {
            if (await PermissionStore.IsGrantedAsync(context.Permission.Name, Name, role))
            {
                return PermissionGrantResult.Granted;
            }
        }

        return PermissionGrantResult.Undefined;
    }
}