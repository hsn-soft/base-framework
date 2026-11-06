using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AppUserDomain.Exceptions;

/// <summary>There is no such user (or the caller may not see it): answered as 404.</summary>
public static class AppUserNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"User {id} was not found.").WithKey("app_user_not_found", id);
}

/// <summary>There is a user with this email already in the tenant.</summary>
public sealed class AppUserEmailDuplicateException : BusinessException
{
    public const string ErrorKey = "app_user_email_duplicate";

    public AppUserEmailDuplicateException(string email)
        : base(ErrorKey, $"A user with the email '{email}' exists.") => WithKey(ErrorKey, email);
}

/// <summary>There is a user with this user name in the tenant already.</summary>
public sealed class AppUserUsernameDuplicateException : BusinessException
{
    public const string ErrorKey = "app_user_username_duplicate";

    public AppUserUsernameDuplicateException(string userName)
        : base(ErrorKey, $"A user named '{userName}' exists.") => WithKey(ErrorKey, userName);
}

/// <summary>A user is made to be told by a text message, and it has no phone number.</summary>
public sealed class AppUserContactMissingException : BusinessException
{
    public const string ErrorKey = "app_user_contact_missing";

    public AppUserContactMissingException(string what)
        : base(ErrorKey, $"The user has no {what} to be told by.") => WithKey(ErrorKey, what);
}

/// <summary>The new password is the temporary one: a temporary password has to be changed to another.</summary>
public sealed class AppUserPasswordNotNewException : BusinessException
{
    public const string ErrorKey = "app_user_password_not_new";

    public AppUserPasswordNotNewException()
        : base(ErrorKey, "The new password has to be another than the temporary one.") => WithKey(ErrorKey);
}

/// <summary>The user signs in with a temporary password whose time is over: it needs a new one (an administrator asks for it).</summary>
public sealed class AppUserTemporaryPasswordExpiredException : BusinessException
{
    public const string ErrorKey = "app_user_temporary_password_expired";

    public AppUserTemporaryPasswordExpiredException()
        : base(ErrorKey, "The temporary password has expired: ask for a new one.") => WithKey(ErrorKey);
}

/// <summary>The current password given to change the password is not the user's.</summary>
public sealed class AppUserWrongPasswordException : BusinessException
{
    public const string ErrorKey = "app_user_wrong_password";

    public AppUserWrongPasswordException()
        : base(ErrorKey, "The current password is wrong.") => WithKey(ErrorKey);
}

/// <summary>A static user (one the system made) is not deleted.</summary>
public sealed class AppUserNotDeletableException : BusinessException
{
    public const string ErrorKey = "app_user_not_deletable";

    public AppUserNotDeletableException(string userName)
        : base(ErrorKey, $"The user '{userName}' is static and cannot be deleted.") => WithKey(ErrorKey, userName);
}
