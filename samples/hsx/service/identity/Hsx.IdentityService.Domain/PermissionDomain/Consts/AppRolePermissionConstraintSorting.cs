using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Consts;

/// <summary>The fields a list of AppRolePermissionConstraint entities may be sorted by and the sorting when it sends none.</summary>
public static class AppRolePermissionConstraintSorting
{
    public const string Default = "id asc";

    public static readonly SortFields<AppRolePermissionConstraint> Fields = new SortFields<AppRolePermissionConstraint>()
        .Add("id", entity => entity.Id);
}
