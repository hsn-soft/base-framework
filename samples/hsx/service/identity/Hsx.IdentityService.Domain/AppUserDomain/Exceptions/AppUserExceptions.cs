using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AppUserDomain.Exceptions;

/// <summary>There is no such user (or the caller may not see it): answered as 404.</summary>
public static class AppUserNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"User {id} was not found.").WithKey("app_user_not_found", id);
}

/// <summary>There is a user with this email already (the email signs in, so it is unique in the system).</summary>
public sealed class AppUserEmailDuplicateException : BusinessException
{
    public const string Code = "app_user_email_duplicate";

    public AppUserEmailDuplicateException(string email)
        : base(Code, $"A user with the email '{email}' exists.") => WithKey(Code, email);
}

/// <summary>There is a user with this user name in the tenant already.</summary>
public sealed class AppUserUserNameDuplicateException : BusinessException
{
    public const string Code = "app_user_user_name_duplicate";

    public AppUserUserNameDuplicateException(string userName)
        : base(Code, $"A user named '{userName}' exists.") => WithKey(Code, userName);
}

/// <summary>The current password given to change the password is not the user's.</summary>
public sealed class AppUserWrongPasswordException : BusinessException
{
    public const string Code = "app_user_wrong_password";

    public AppUserWrongPasswordException()
        : base(Code, "The current password is wrong.") => WithKey(Code);
}
