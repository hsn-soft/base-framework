using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;

namespace Hsx.IdentityService.Domain.AppUserRoleDomain.Consts;

/// <summary>The fields a list of AppUserRole entities may be sorted by and the sorting when it sends none.</summary>
public static class AppUserRoleSorting
{
    public const string Default = "id asc";

    public static readonly SortFields<AppUserRole> Fields = new SortFields<AppUserRole>()
        .Add("id", entity => entity.Id);
}
