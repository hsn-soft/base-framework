using HsnSoft.Base.Domain.Sorting;
using Xyz.ShipmentService.Domain.PaymentDomain.Entities;

namespace Xyz.ShipmentService.Domain.PaymentDomain.Repositories;

/// <summary>The fields a list of payments may be sorted by (<c>"amount desc"</c>) and the sorting when it sends none: the latest first.</summary>
public static class PaymentSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<Payment> Fields = new SortFields<Payment>()
        .Add("amount", payment => payment.Amount)
        .Add("status", payment => payment.Status)
        .Add("createdAt", payment => payment.CreatedAt);
}
