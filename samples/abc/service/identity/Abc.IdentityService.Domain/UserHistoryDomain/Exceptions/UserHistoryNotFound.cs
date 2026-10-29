using HsnSoft.Base;

namespace Abc.IdentityService.Domain.UserHistoryDomain.Exceptions;

/// <summary>There is no such history record: answered as 404.</summary>
public static class UserHistoryNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"History record {id} was not found.").WithKey("user_history_not_found", id);
}
