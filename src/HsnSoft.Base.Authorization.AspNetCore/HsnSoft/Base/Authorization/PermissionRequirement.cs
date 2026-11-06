using Microsoft.AspNetCore.Authorization;

namespace HsnSoft.Base.Authorization;

/// <summary>What the policy of a permission asks for: that the caller was granted <see cref="Permission"/>.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;

    public override string ToString() => $"PermissionRequirement: {Permission}";
}
