using Abc.IdentityService.Domain.UserHistoryDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Abc.IdentityService.Domain.UserHistoryDomain.Entities;

/// <summary>
/// One thing that happened to a user (made, changed, deleted), kept in the history database: who did it and when come from <see cref="CreationAuditedEntity{TKey}"/>, the tenant is the one of the caller.
/// The user is only referenced by its id: the history lives in another database than the users.
/// </summary>
public sealed class UserHistory : CreationAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private UserHistory()
    {
    }

    public Guid UserId { get; private set; }

    /// <summary>The name the user had then (it may change later).</summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary><see cref="UserHistoryConsts.Created"/>, <see cref="UserHistoryConsts.Updated"/> or <see cref="UserHistoryConsts.Deleted"/>.</summary>
    public string Action { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public static UserHistory Record(Guid userId, string userName, string action)
        => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = Guard.NotDefault<Guid>(userId, nameof(UserId)),
            UserName = Guard.NotNullOrWhiteSpace(userName, nameof(UserName), UserHistoryConsts.UserNameMaxLength),
            Action = Guard.NotNullOrWhiteSpace(action, nameof(Action), UserHistoryConsts.ActionMaxLength),
        };
}
