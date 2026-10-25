using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Xyz.AuthServer.Domain.UserHistoryDomain.Consts;

namespace Xyz.AuthServer.Domain.UserHistoryDomain.Entities;

/// <summary>
/// One thing that happened to a user (made, changed, deleted, signed in), kept in the history database: who did it and when come from <see cref="FullAuditedEntity{TKey}"/> (the soft delete too), the tenant is the one of the caller
/// or the one given. The user is only referenced by its id: the history lives in another database than the users.
/// </summary>
public sealed class UserHistory : FullAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private UserHistory()
    {
    }

    public Guid UserId { get; private set; }

    /// <summary>The name the user had then (it may change later).</summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary>The user name in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedUserName { get; private set; } = string.Empty;

    /// <summary><see cref="UserHistoryConsts.Created"/>, <see cref="UserHistoryConsts.Updated"/>, <see cref="UserHistoryConsts.Deleted"/>, <see cref="UserHistoryConsts.Login"/> or any other word of at most <see cref="UserHistoryConsts.ActionMaxLength"/> characters.</summary>
    public string Action { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public Guid TenantId { get; private set; }

    /// <param name="userId"></param>

    /// <param name="userName"></param>
    /// <param name="action"></param>
    /// <param name="note"></param>    /// <param name="tenantId">The tenant of the user, when the one who records is not in it (the auth server is nobody's yet); the tenant of the caller when it is not given.</param>
    public static UserHistory Record(Guid userId, string userName, string action, string? note = null, Guid? tenantId = null)
    {
        UserHistory history = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = Guard.NotDefault<Guid>(userId, nameof(UserId)),
            UserName = Guard.NotNullOrWhiteSpace(userName, nameof(UserName), UserHistoryConsts.UserNameMaxLength),
            TenantId = tenantId ?? Guid.Empty,
        };
        history.NormalizedUserName = StringHelper.Normalize(history.UserName);
        history.Change(action, note);
        return history;
    }

    public void Update(string action, string? note) => Change(action, note);

    private void Change(string action, string? note)
    {
        // a word, in small letters, so a list can ask for it by name
        Action = Guard.NotNullOrWhiteSpace(action, nameof(Action), UserHistoryConsts.ActionMaxLength).Trim().ToLowerInvariant();
        Note = string.IsNullOrWhiteSpace(note) ? null : Guard.Length(note.Trim(), nameof(Note), UserHistoryConsts.NoteMaxLength);
    }
}
