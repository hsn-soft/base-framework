using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HsnSoft.Base.Authorization;

/// <summary>
/// The permissions of a system are the public string constants of its classes, nested by service and by controller (<c>ShopPermissions.Orders.Read</c>): the code names the permission in the attribute of an endpoint and is the only list of them there is.
/// The service that manages the grants makes its catalogue from the same constants, so a permission an endpoint asks for is always one that can be granted.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>Every public string constant of <paramref name="root"/> and of the classes nested in it, at any depth, in the order they are declared.</summary>
    public static IReadOnlyList<string> CodesOf(Type root)
    {
        ArgumentNullException.ThrowIfNull(root);

        List<string> codes = [];
        Collect(root, codes);
        return codes;
    }

    private static void Collect(Type type, List<string> codes)
    {
        codes.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!));
        foreach (Type nested in type.GetNestedTypes(BindingFlags.Public))
        {
            Collect(nested, codes);
        }
    }
}
