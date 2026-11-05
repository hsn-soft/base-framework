using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Hsx.IdentityService.Domain.AppRoleDomain.Consts;

/// <summary>The fields a list of roles may be sorted by and the sorting when it sends none.</summary>
public static class AppRoleSorting
{
    public const string Default = "name asc";

    public static readonly SortFields<AppRole> Fields = new SortFields<AppRole>()
        .Add("name", role => role.NormalizedName)
        .Add("createdAt", role => role.CreatedAt);
}
