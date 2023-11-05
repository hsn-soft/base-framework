using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions;

public class RolePermissionValueProvider : PermissionValueProvider
{
    public const string ProviderName = "R";

    public override string Name => ProviderName;
    protected ICurrentTenant CurrentTenant { get; }

    public RolePermissionValueProvider(IPermissionStore permissionStore, ICurrentTenant currentTenant) : base(permissionStore)
    {
        CurrentTenant = currentTenant;
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
            //return PermissionGrantResult.Undefined;

            var clientId = context.Principal?.FindFirst(BaseClaimTypes.ClientId)?.Value ?? context.Principal?.FindFirst("client_id")?.Value;

            if (clientId == null)
            {
                return PermissionGrantResult.Undefined;
            }

            using (CurrentTenant.Change(null))
            {
                return await PermissionStore.IsGrantedAsync(context.Permission.Name, Name, clientId)
                    ? PermissionGrantResult.Granted
                    : PermissionGrantResult.Undefined;
            }
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