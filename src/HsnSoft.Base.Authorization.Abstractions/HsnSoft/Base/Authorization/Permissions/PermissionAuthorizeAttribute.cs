using Microsoft.AspNetCore.Authorization;

namespace HsnSoft.Base.Authorization.Permissions;

public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(string permission) => Policy = $"{PermissionConsts.Prefix}:{permission}";
}

public static class PermissionConsts
{
    public const string Prefix = "permission";
}