using HsnSoft.Base.Domain.Repositories;
using Xyz.ShipmentService.Domain.PaymentDomain.Entities;

namespace Xyz.ShipmentService.Application.Services;

/// <summary>What the payment of an order came to.</summary>
public sealed record PaymentResult(Guid PaymentId, bool IsSuccessful, string? Reason);

/// <summary>
/// Takes the money of an order from its customer. The payment provider is simulated: it declines when the address of the order says <c>nopay</c> (a card that is refused), and takes the money otherwise. An order is paid once: asking again
/// answers with what the first payment came to (a message that comes twice), and a payment left pending by a failure in the middle is finished.
/// </summary>
public sealed class PaymentProcessor(IRepository<Payment, Guid> payments)
{
    public async Task<PaymentResult> ProcessAsync(Guid orderId, Guid customerId, decimal amount, string shippingAddress, CancellationToken cancellationToken = default)
    {
        Payment? payment = await payments.FindAsync(existing => existing.OrderId == orderId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            payment = Payment.Create(orderId, customerId, amount);
            await payments.AddAsync(payment, cancellationToken).ConfigureAwait(false);
        }

        if (payment.Status == PaymentStatus.Pending)
        {
            if (shippingAddress.Contains("nopay", StringComparison.OrdinalIgnoreCase))
            {
                payment.Fail("The card was declined.");
            }
            else
            {
                payment.Succeed($"TX-{Guid.NewGuid():N}"[..19].ToUpperInvariant());
            }

            await payments.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
        }

        return new PaymentResult(payment.Id, payment.Status == PaymentStatus.Succeeded, payment.FailureReason);
    }
}
