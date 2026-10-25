using HsnSoft.Base.Domain.Sorting;
using Xyz.AuthServer.Domain.UserHistoryDomain.Entities;

namespace Xyz.AuthServer.Domain.UserHistoryDomain.Repositories;

/// <summary>The fields a list of the history may be sorted by (the names a client sends, <c>"createdAt desc, action"</c>) and the sorting when it sends none: the latest first.</summary>
public static class UserHistorySorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<UserHistory> Fields = new SortFields<UserHistory>()
        .Add("userName", history => history.NormalizedUserName)
        .Add("action", history => history.Action)
        .Add("createdAt", history => history.CreatedAt);
}
