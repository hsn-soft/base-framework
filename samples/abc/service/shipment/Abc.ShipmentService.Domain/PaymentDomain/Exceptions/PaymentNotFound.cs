using HsnSoft.Base;

namespace Abc.ShipmentService.Domain.PaymentDomain.Exceptions;

/// <summary>There is no such payment: answered as 404.</summary>
public static class PaymentNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Payment {id} was not found.").WithKey("payment_not_found", id);
}
