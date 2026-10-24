using HsnSoft.Base;

namespace Abc.IdentityService.Domain.UserDomain.Exceptions;

/// <summary>The user name is taken (a known error: the client is told, with the code <see cref="Code"/>).</summary>
public sealed class UserUserNameDuplicateException(string userName) : BusinessException(Code, $"The user name '{userName}' is taken.")
{
    public const string Code = "user_name_duplicate";
}
