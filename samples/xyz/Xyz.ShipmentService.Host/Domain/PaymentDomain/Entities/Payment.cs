using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Xyz.ShipmentService.Domain.PaymentDomain.Consts;

namespace Xyz.ShipmentService.Domain.PaymentDomain.Entities;

/// <summary>
/// The payment of an order: what was asked of a customer and how it ended. A payment is made <see cref="PaymentStatus.Pending"/> and ends as <see cref="PaymentStatus.Succeeded"/> or <see cref="PaymentStatus.Failed"/>; a succeeded one can be
/// <see cref="PaymentStatus.Refunded"/>. The audit fields and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class Payment : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private Payment()
    {
    }

    public Guid OrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>The ISO code of the currency (<c>TRY</c>).</summary>
    public string Currency { get; private set; } = PaymentConsts.DefaultCurrency;

    public PaymentStatus Status { get; private set; }

    /// <summary>What the payment provider answered with when it took the money.</summary>
    public string? Reference { get; private set; }

    /// <summary>Why it failed.</summary>
    public string? FailureReason { get; private set; }

    public Guid TenantId { get; private set; }

    public static Payment Create(Guid orderId, Guid customerId, decimal amount, string? currency = null)
    {
        if (amount <= 0)
        {
            throw new DomainException("A payment is for an amount above zero.").WithKey("payment_amount_invalid");
        }

        return new Payment
        {
            Id = Guid.CreateVersion7(),
            OrderId = Guard.NotDefault<Guid>(orderId, nameof(OrderId)),
            CustomerId = Guard.NotDefault<Guid>(customerId, nameof(CustomerId)),
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? PaymentConsts.DefaultCurrency : Guard.Length(currency.Trim().ToUpperInvariant(), nameof(Currency), PaymentConsts.CurrencyLength, PaymentConsts.CurrencyLength)!,
            Status = PaymentStatus.Pending,
        };
    }

    /// <summary>The provider took the money. A payment that is not pending cannot succeed; one that succeeded already stays as it is (a message that came twice).</summary>
    public void Succeed(string reference)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            return;
        }

        EnsurePending();
        Status = PaymentStatus.Succeeded;
        Reference = Guard.NotNullOrWhiteSpace(reference, nameof(Reference), PaymentConsts.ReferenceMaxLength);
        FailureReason = null;
    }

    /// <summary>The provider refused. A payment that is not pending cannot fail; one that failed already stays as it is.</summary>
    public void Fail(string reason)
    {
        if (Status == PaymentStatus.Failed)
        {
            return;
        }

        EnsurePending();
        Status = PaymentStatus.Failed;
        FailureReason = Guard.NotNullOrWhiteSpace(reason, nameof(FailureReason), PaymentConsts.ReasonMaxLength);
    }

    /// <summary>Corrects a payment by hand (a refund, a settlement the provider reported late): pending becomes any other status, a succeeded payment can only be refunded, a failed or refunded one is final.</summary>
    public void ChangeStatus(PaymentStatus status, string? reason)
    {
        if (status == Status)
        {
            return;
        }

        bool allowed = Status switch
        {
            PaymentStatus.Pending => status != PaymentStatus.Pending,
            PaymentStatus.Succeeded => status == PaymentStatus.Refunded,
            _ => false,
        };
        if (!allowed)
        {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} payment cannot become {status.ToString().ToLowerInvariant()}.").WithKey("payment_status_final", Status.ToString(), status.ToString());
        }

        Status = status;
        FailureReason = status == PaymentStatus.Failed ? Guard.NotNullOrWhiteSpace(reason, nameof(FailureReason), PaymentConsts.ReasonMaxLength) : FailureReason;
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} payment cannot change.").WithKey("payment_status_final", Status.ToString(), "Pending");
        }
    }
}
