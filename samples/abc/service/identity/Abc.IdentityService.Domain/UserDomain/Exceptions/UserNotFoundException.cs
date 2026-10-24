using HsnSoft.Base;

namespace Abc.IdentityService.Domain.UserDomain.Exceptions;

/// <summary>There is no such user: answered as 404.</summary>
public static class UserNotFound
{
    public static NotFoundException For(Guid id) => new($"User {id} was not found.");
}
