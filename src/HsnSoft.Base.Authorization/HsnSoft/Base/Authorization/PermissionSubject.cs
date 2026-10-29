namespace HsnSoft.Base.Authorization;

/// <summary>Who a grant is for: a user (<see cref="PermissionProviders.User"/>, the key is the id of the user) or a role (<see cref="PermissionProviders.Role"/>, the key is the name of the role).</summary>
public readonly record struct PermissionSubject(string Provider, string Key);

/// <summary>The kinds of subject a grant can be for.</summary>
public static class PermissionProviders
{
    public const string User = "user";
    public const string Role = "role";
}
