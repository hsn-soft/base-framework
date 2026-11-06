using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;

namespace Hsx.IdentityService.Domain.AppUserDomain.Consts;

/// <summary>The fields a list of users may be sorted by and the sorting when it sends none.</summary>
public static class AppUserSorting
{
    public const string Default = "userName asc";

    public static readonly SortFields<AppUser> Fields = new SortFields<AppUser>()
        .Add("userName", user => user.NormalizedUserName)
        .Add("email", user => user.NormalizedEmail)
        .Add("displayName", user => user.DisplayName)
        .Add("createdAt", user => user.CreatedAt);
}
