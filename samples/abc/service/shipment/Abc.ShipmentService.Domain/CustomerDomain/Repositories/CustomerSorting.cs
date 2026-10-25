using Abc.ShipmentService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.Domain.Sorting;

namespace Abc.ShipmentService.Domain.CustomerDomain.Repositories;

/// <summary>The fields a list of customers may be sorted by (<c>"fullName, createdAt desc"</c>) and the sorting when it sends none.</summary>
public static class CustomerSorting
{
    public const string Default = "userName asc";

    public static readonly SortFields<Customer> Fields = new SortFields<Customer>()
        .Add("userName", customer => customer.NormalizedUserName)
        .Add("fullName", customer => customer.FullName)
        .Add("email", customer => customer.Email)
        .Add("createdAt", customer => customer.CreatedAt);
}
