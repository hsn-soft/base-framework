using Microsoft.AspNetCore.Authorization;

namespace HsnSoft.Base.Authorization;

public static class AuthorizationPolicyBuilderExtension
{
    public static AuthorizationPolicyBuilder RequireUserPermission(this AuthorizationPolicyBuilder builder, string permissionType)
    {
        builder.AddRequirements(new PermissionRequirement(permissionType));
        return builder;
    }
}