using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.MenuDomain.Entities;

namespace Hsx.IdentityService.Domain.MenuDomain.Consts;

/// <summary>The fields a list of AppMenuPermission entities may be sorted by and the sorting when it sends none.</summary>
public static class AppMenuPermissionSorting
{
    public const string Default = "sortOrder asc";

    public static readonly SortFields<AppMenuPermission> Fields = new SortFields<AppMenuPermission>()
        .Add("sortOrder", entity => entity.SortOrder)
        .Add("id", entity => entity.Id);
}
