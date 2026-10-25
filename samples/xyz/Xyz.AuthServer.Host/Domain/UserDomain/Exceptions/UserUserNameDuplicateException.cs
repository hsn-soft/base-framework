using HsnSoft.Base;

namespace Xyz.AuthServer.Domain.UserDomain.Exceptions;

/// <summary>The user name is taken (a known error: the client is told, with the code <see cref="Code"/>).</summary>
public sealed class UserUserNameDuplicateException : BusinessException
{
    public const string Code = "user_name_duplicate";

    public UserUserNameDuplicateException(string userName)
        : base(Code, $"The user name '{userName}' is taken.") => WithKey(Code, userName);
}
