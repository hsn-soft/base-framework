using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace HsnSoft.Base.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement([NotNull] string permissionName)
    {
        Check.NotNull(permissionName, nameof(permissionName));

        PermissionName = permissionName;
    }

    public string PermissionName { get; }

    public override string ToString()
    {
        return $"PermissionRequirement: {PermissionName}";
    }
}