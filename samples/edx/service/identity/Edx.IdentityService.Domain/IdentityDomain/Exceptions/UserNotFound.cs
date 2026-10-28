using HsnSoft.Base;

namespace Edx.IdentityService.Domain.IdentityDomain.Exceptions;

/// <summary>There is no such user (or it is another tenant's): answered as 404.</summary>
public static class UserNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"User {id} was not found.").WithKey("user_not_found", id);
}

/// <summary>The user name is taken in the tenant (a known error: the client is told, with the code <see cref="Code"/>).</summary>
public sealed class UserNameDuplicateException : BusinessException
{
    public const string Code = "user_name_duplicate";

    public UserNameDuplicateException(string userName)
        : base(Code, $"The user name '{userName}' is taken.") => WithKey(Code, userName);
}

/// <summary>The role name is taken in the tenant (a known error).</summary>
public sealed class RoleNameDuplicateException : BusinessException
{
    public const string Code = "role_name_duplicate";

    public RoleNameDuplicateException(string name)
        : base(Code, $"The role '{name}' exists already.") => WithKey(Code, name);
}

/// <summary>A role that a user has is not deleted (a known error).</summary>
public sealed class RoleInUseException : BusinessException
{
    public const string Code = "role_in_use";

    public RoleInUseException(string name)
        : base(Code, $"The role '{name}' is given to a user: take it from them first.") => WithKey(Code, name);
}

/// <summary>A caller does not delete itself (a known error).</summary>
public sealed class UserDeletesItselfException : BusinessException
{
    public const string Code = "user_deletes_itself";

    public UserDeletesItselfException()
        : base(Code, "You cannot delete the user you are signed in as.") => WithKey(Code);
}
