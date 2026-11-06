using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;

namespace Hsx.IdentityService.Domain.CompanyDomain.Consts;

/// <summary>The fields a list of companies may be sorted by and the sorting when it sends none.</summary>
public static class CompanySorting
{
    public const string Default = "name asc";

    public static readonly SortFields<Company> Fields = new SortFields<Company>()
        .Add("title", entity => entity.Title)
        .Add("name", entity => entity.NormalizedName)
        .Add("createdAt", entity => entity.CreatedAt);
}
