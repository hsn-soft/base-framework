using HsnSoft.Base;

namespace Edx.IdentityService.Domain.IdentityDomain.Exceptions;

/// <summary>There is no such user (or it is another tenant's): answered as 404.</summary>
public static class UserNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"User {id} was not found.").WithKey("user_not_found", id);
}
