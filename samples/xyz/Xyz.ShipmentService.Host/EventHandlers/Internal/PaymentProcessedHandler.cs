using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using Xyz.Shared.Contracts.Events.Shipment;
using Xyz.ShipmentService.Application.Contracts.Events;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Xyz.ShipmentService.Domain.ShipmentDomain.Entities;

namespace Xyz.ShipmentService.EventHandlers.Internal;

/// <summary>
/// The payment of an order was processed (an event of this service, <see cref="PaymentProcessedEto"/>). When it succeeded: makes the shipment and sends it (the carrier is simulated: the parcel is delivered, unless the address says <c>fail</c>, which the
/// carrier cannot reach). When it was declined nothing is sent. Either way the order service is told how it ended (<see cref="ShipmentCompletedEto"/>). It runs as the user who made the order, in the tenant of the order (the envelope says who). The shipment and its
/// history are made by the application service, as for a request. A message that comes twice finds the shipment of the order and finishes what was left.
/// </summary>
public sealed class PaymentProcessedHandler(IShipmentAppService shipmentService, IRepository<Shipment, Guid> shipments, IEventBus eventBus) : IIntegrationEventHandler<PaymentProcessedEto>
{
    private const string Carrier = "Xyz Express";

    public async Task HandleAsync(MessageEnvelope<PaymentProcessedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        PaymentProcessedEto payment = envelope.Message;
        if (!payment.IsSuccessful)
        {
            await eventBus.PublishAsync(new ShipmentCompletedEto(payment.OrderId, null, null, false, $"Payment failed: {payment.Reason ?? "the payment was declined"}"), cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        Shipment? existing = await shipments.FindAsync(shipment => shipment.OrderId == payment.OrderId, cancellationToken).ConfigureAwait(false);
        Guid id;
        string tracking;
        ShipmentStatus status;
        if (existing is null)
        {
            ShipmentDto created = await shipmentService.CreateAsync(new ShipmentCreateDto { OrderId = payment.OrderId, Carrier = Carrier, Address = payment.ShippingAddress }, cancellationToken).ConfigureAwait(false);
            (id, tracking, status) = (created.Id, created.TrackingNumber, ShipmentStatus.Preparing);
        }
        else
        {
            (id, tracking, status) = (existing.Id, existing.TrackingNumber, existing.Status);
        }

        bool reachable = !payment.ShippingAddress.Contains("fail", StringComparison.OrdinalIgnoreCase);
        if (status == ShipmentStatus.Preparing)
        {
            await MoveAsync(id, payment.ShippingAddress, ShipmentStatus.InTransit, cancellationToken).ConfigureAwait(false);
            status = ShipmentStatus.InTransit;
        }

        if (status == ShipmentStatus.InTransit)
        {
            status = reachable ? ShipmentStatus.Delivered : ShipmentStatus.Returned;
            await MoveAsync(id, payment.ShippingAddress, status, cancellationToken).ConfigureAwait(false);
        }

        bool delivered = status == ShipmentStatus.Delivered;
        ShipmentCompletedEto completed = new(payment.OrderId, id, tracking, delivered, delivered ? null : "the carrier could not reach the address");
        await eventBus.PublishAsync(completed, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Task MoveAsync(Guid id, string address, ShipmentStatus status, CancellationToken cancellationToken)
        => shipmentService.UpdateAsync(new ShipmentUpdateDto { Id = id, Carrier = Carrier, Address = address, Status = status.ToString() }, cancellationToken);
}
