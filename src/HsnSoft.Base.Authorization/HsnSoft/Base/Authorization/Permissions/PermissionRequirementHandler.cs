using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HsnSoft.Base.Authorization.Permissions;

public class PermissionRequirementHandler(IPermissionChecker permissionChecker) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (await permissionChecker.IsGrantedAsync(context.User, requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}