using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public class UserPermissionValueProvider(IPermissionStore permissionStore) : PermissionValueProvider(permissionStore)
{
    public override string Name => PermissionProviders.User;

    public override async Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        string userId = context.Principal?.FindFirst(BaseClaimTypes.UserId)?.Value ?? context.Principal?.FindFirst("sub")?.Value;

        if (userId == null)
        {
            return PermissionGrantResult.Undefined;
        }

        return await PermissionStore.IsGrantedAsync(context.Permission, Name, userId)
            ? PermissionGrantResult.Granted
            : PermissionGrantResult.Undefined;
    }
}