using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HsnSoft.Base.Authorization;

/// <summary>Meets a <see cref="PermissionRequirement"/> when the checker says the caller was granted the permission; otherwise it says nothing, and the request is refused (403).</summary>
public sealed class PermissionRequirementHandler(IPermissionChecker checker) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (await checker.IsGrantedAsync(requirement.Permission).ConfigureAwait(false))
        {
            context.Succeed(requirement);
        }
    }
}
