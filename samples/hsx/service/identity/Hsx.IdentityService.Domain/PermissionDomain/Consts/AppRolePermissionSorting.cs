using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Consts;

/// <summary>The fields a list of AppRolePermission entities may be sorted by and the sorting when it sends none.</summary>
public static class AppRolePermissionSorting
{
    public const string Default = "id asc";

    public static readonly SortFields<AppRolePermission> Fields = new SortFields<AppRolePermission>()
        .Add("id", entity => entity.Id);
}
