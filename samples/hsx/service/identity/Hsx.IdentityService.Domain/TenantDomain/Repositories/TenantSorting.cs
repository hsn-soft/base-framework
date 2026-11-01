using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.TenantDomain.Entities;

namespace Hsx.IdentityService.Domain.TenantDomain.Repositories;

/// <summary>The fields a list of tenants may be sorted by and the sorting when it sends none.</summary>
public static class TenantSorting
{
    public const string Default = "name asc";

    public static readonly SortFields<Tenant> Fields = new SortFields<Tenant>()
        .Add("name", tenant => tenant.NormalizedName)
        .Add("title", tenant => tenant.Title)
        .Add("tenantType", tenant => tenant.TenantType)
        .Add("createdAt", tenant => tenant.CreatedAt);
}
