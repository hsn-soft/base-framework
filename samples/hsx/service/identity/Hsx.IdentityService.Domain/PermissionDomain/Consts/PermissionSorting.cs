using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.PermissionDomain.Consts;

/// <summary>The fields a list of Permission entities may be sorted by and the sorting when it sends none.</summary>
public static class PermissionSorting
{
    public const string Default = "code asc";

    public static readonly SortFields<Permission> Fields = new SortFields<Permission>()
        .Add("code", entity => entity.Code)
        .Add("name", entity => entity.Name);
}
