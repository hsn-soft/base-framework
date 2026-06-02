using System.Linq;
using System.Threading.Tasks;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public class RoleConstraintValueProvider(IPermissionConstraintStore permissionConstraintStore) : PermissionConstraintValueProvider(permissionConstraintStore)
{
    public override string Name => PermissionProviders.Role;

    public override async Task<PermissionConstraintValueResult> CheckAsync(PermissionConstraintCheckContext context)
    {
        string[] roles = context.Principal?.FindAll(BaseClaimTypes.Role).Select(c => c.Value).ToArray();

        if (roles == null || roles.Length == 0)
        {
            roles = context.Principal?.FindAll("role").Select(c => c.Value).ToArray();
        }

        if (roles == null || roles.Length == 0)
        {
            string clientId = context.Principal?.FindFirst(BaseClaimTypes.ClientId)?.Value;
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return PermissionConstraintValueResult.NotFound;
            }

            string value = await PermissionConstraintStore.GetValueAsync(context.Constraint, PermissionProviders.Client, clientId);

            return string.IsNullOrWhiteSpace(value)
                ? PermissionConstraintValueResult.NotFound
                : PermissionConstraintValueResult.Found(value);
        }

        foreach (string role in roles.Distinct())
        {
            string value = await PermissionConstraintStore.GetValueAsync(context.Constraint, Name, role);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return PermissionConstraintValueResult.Found(value);
            }
        }

        return PermissionConstraintValueResult.NotFound;
    }
}