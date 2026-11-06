using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.MenuDomain.Entities;

namespace Hsx.IdentityService.Domain.MenuDomain.Consts;

/// <summary>The fields a list of AppMenu entities may be sorted by and the sorting when it sends none.</summary>
public static class AppMenuSorting
{
    public const string Default = "sortOrder asc";

    public static readonly SortFields<AppMenu> Fields = new SortFields<AppMenu>()
        .Add("code", entity => entity.Code)
        .Add("title", entity => entity.Title)
        .Add("sortOrder", entity => entity.SortOrder);
}
