using HsnSoft.Base.Domain.Sorting;
using Xyz.AuthServer.Domain.UserDomain.Entities;

namespace Xyz.AuthServer.Domain.UserDomain.Repositories;

/// <summary>The fields a list of users may be sorted by (the names a client sends, <c>"userName asc, createdAt desc"</c>) and the sorting when it sends none.</summary>
public static class UserSorting
{
    public const string Default = "userName asc";

    public static readonly SortFields<User> Fields = new SortFields<User>()
        .Add("userName", user => user.NormalizedUserName)
        .Add("email", user => user.NormalizedEmail)
        .Add("displayName", user => user.DisplayName)
        .Add("isActive", user => user.IsActive)
        .Add("createdAt", user => user.CreatedAt);
}
