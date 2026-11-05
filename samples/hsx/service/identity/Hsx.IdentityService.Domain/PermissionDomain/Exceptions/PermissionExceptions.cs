using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.PermissionDomain.Exceptions;

/// <summary>There is no such permission, or it is not active any more: answered as 404.</summary>
public static class PermissionNotFound
{
    public static NotFoundException For(string code) => new NotFoundException($"Permission '{code}' was not found.").WithKey("permission_not_found", code);
}

/// <summary>A constraint is granted without its value.</summary>
public sealed class PermissionValueRequiredException : BusinessException
{
    public const string ErrorKey = "permission_value_required";

    public PermissionValueRequiredException(string permission)
        : base(ErrorKey, $"The permission '{permission}' limits a value: give it one.") => WithKey(ErrorKey, permission);
}

/// <summary>A permission that limits no value is granted with one.</summary>
public sealed class PermissionValueNotAllowedException : BusinessException
{
    public const string ErrorKey = "permission_value_not_allowed";

    public PermissionValueNotAllowedException(string permission)
        : base(ErrorKey, $"The permission '{permission}' limits no value: it takes none.") => WithKey(ErrorKey, permission);
}
