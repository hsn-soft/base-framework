using HsnSoft.Base.Authorization;

namespace Hsx.Shared.Contracts.Permissions;

/// <summary>
/// Every permission of the Hsx system: the constants of the permission class of each service (<see cref="IdentityPermissions"/>, <see cref="ContentPermissions"/>, ...). A service writes its own codes in its class; the identity service makes its catalogue
/// of permissions from <see cref="Codes"/>, so an endpoint can only ask for a permission that can be granted. A service that gets endpoints adds its class to <see cref="Services"/>.
/// </summary>
public static class HsxPermissionCatalog
{
    /// <summary>The permission class of each service.</summary>
    public static IReadOnlyList<Type> Services { get; } =
    [
        typeof(IdentityPermissions),
        typeof(ContentPermissions),
        typeof(TextNormalizerPermissions),
        typeof(VideoGeneratorPermissions),
    ];

    /// <summary>The codes of every service, in the order of <see cref="Services"/>.</summary>
    public static IReadOnlyList<string> Codes { get; } = [.. Services.SelectMany(PermissionCatalog.CodesOf)];

    /// <summary>The codes that let a user into a page (<c>page.</c>).</summary>
    public static IReadOnlyList<string> Pages { get; } = [.. Codes.Where(code => code.StartsWith("page.", StringComparison.Ordinal))];

    /// <summary>The codes of the actions of the endpoints (<c>endpoint.</c>).</summary>
    public static IReadOnlyList<string> Endpoints { get; } = [.. Codes.Where(code => code.StartsWith("endpoint.", StringComparison.Ordinal))];

    /// <summary>The codes of what happens inside an endpoint (<c>operation.</c>).</summary>
    public static IReadOnlyList<string> Operations { get; } = [.. Codes.Where(code => code.StartsWith("operation.", StringComparison.Ordinal))];

    /// <summary>The codes of the limits a role is given a value for (<c>constraint.</c>).</summary>
    public static IReadOnlyList<string> Constraints { get; } = [.. Codes.Where(code => code.StartsWith("constraint.", StringComparison.Ordinal))];
}
