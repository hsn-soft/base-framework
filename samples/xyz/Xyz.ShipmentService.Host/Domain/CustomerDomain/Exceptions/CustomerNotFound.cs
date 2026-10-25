using HsnSoft.Base;

namespace Xyz.ShipmentService.Domain.CustomerDomain.Exceptions;

/// <summary>There is no such customer: answered as 404.</summary>
public static class CustomerNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Customer {id} was not found.").WithKey("customer_not_found", id);
}
