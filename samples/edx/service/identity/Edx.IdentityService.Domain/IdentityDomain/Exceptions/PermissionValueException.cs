using HsnSoft.Base;

namespace Edx.IdentityService.Domain.IdentityDomain.Exceptions;

/// <summary>A grant has a value it must not have, or lacks the one it must have (a known error: the client is told, with one of the codes below).</summary>
public sealed class PermissionValueException : BusinessException
{
    public const string Required = "permission_value_required";
    public const string NotAllowed = "permission_value_not_allowed";

    public PermissionValueException(string code, string permission)
        : base(code, code == Required ? $"The permission '{permission}' limits a value: give it one." : $"The permission '{permission}' limits no value: it takes none.")
        => WithKey(code, permission);
}
