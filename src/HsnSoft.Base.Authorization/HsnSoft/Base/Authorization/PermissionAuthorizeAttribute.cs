using System;
using Microsoft.AspNetCore.Authorization;

namespace HsnSoft.Base.Authorization;

/// <summary>
/// Lets a caller in only when it was granted the permission (<c>[PermissionAuthorize(EdxPermissions.FirmInvoice.Files.Read)]</c>): the user, or one of the roles of the user, in the scope of the request (see <see cref="IPermissionChecker"/>).
/// A caller that is not signed in is answered 401, one that was not granted is answered 403.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        Policy = PermissionPolicies.Prefix + permission;
    }
}

/// <summary>The name of the authorization policy of a permission: a prefix and the code.</summary>
public static class PermissionPolicies
{
    public const string Prefix = "permission:";
}
