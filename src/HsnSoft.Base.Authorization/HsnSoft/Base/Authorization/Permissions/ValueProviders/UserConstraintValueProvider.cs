using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public class UserConstraintValueProvider(IPermissionConstraintStore permissionConstraintStore) : PermissionConstraintValueProvider(permissionConstraintStore)
{
    public override string Name => PermissionProviders.User;

    public override async Task<PermissionConstraintValueResult> CheckAsync(PermissionConstraintCheckContext context)
    {
        string userId = context.Principal?.FindFirst(BaseClaimTypes.UserId)?.Value ?? context.Principal?.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return PermissionConstraintValueResult.NotFound;
        }

        string value = await PermissionConstraintStore.GetValueAsync(context.Constraint, Name, userId);

        return string.IsNullOrWhiteSpace(value)
            ? PermissionConstraintValueResult.NotFound
            : PermissionConstraintValueResult.Found(value);
    }
}