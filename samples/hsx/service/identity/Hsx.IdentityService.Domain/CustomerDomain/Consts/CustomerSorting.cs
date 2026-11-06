using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;

namespace Hsx.IdentityService.Domain.CustomerDomain.Consts;

/// <summary>The fields a list of customers may be sorted by and the sorting when it sends none.</summary>
public static class CustomerSorting
{
    public const string Default = "domain asc";

    public static readonly SortFields<Customer> Fields = new SortFields<Customer>()
        .Add("domain", entity => entity.NormalizedDomain)
        .Add("createdAt", entity => entity.CreatedAt);
}
