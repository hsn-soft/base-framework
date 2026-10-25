using Abc.Shared.Contracts.Events.Product;
using Abc.Shared.Contracts.Events.Shipment;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;

namespace Abc.ShipmentService.Http.Host.EventHandlers.Product;

/// <summary>
/// The stock of an order was set aside (the product service published it for whoever cares): makes the shipment, sends it (the carrier is simulated: the parcel is delivered, unless the address says <c>fail</c>, which the carrier cannot reach), and tells the order service how it ended
/// (<see cref="ShipmentCompletedEto"/>). It runs as the user who made the order, in the tenant of the order (the envelope says who). The shipment and its history are made by the application service, as for a request. A message that comes twice finds the shipment of the order and finishes what was left.
/// </summary>
public sealed class OrderStockReservedHandler(IShipmentAppService shipmentService, IRepository<Shipment, Guid> shipments, IEventBus eventBus) : IIntegrationEventHandler<OrderStockReservedEto>
{
    private const string Carrier = "Abc Express";

    public async Task HandleAsync(MessageEnvelope<OrderStockReservedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OrderStockReservedEto request = envelope.Message;
        Shipment? existing = await shipments.FindAsync(shipment => shipment.OrderId == request.OrderId, cancellationToken).ConfigureAwait(false);
        Guid id;
        string tracking;
        ShipmentStatus status;
        if (existing is null)
        {
            ShipmentDto created = await shipmentService.CreateAsync(new ShipmentCreateDto { OrderId = request.OrderId, Carrier = Carrier, Address = request.ShippingAddress }, cancellationToken).ConfigureAwait(false);
            (id, tracking, status) = (created.Id, created.TrackingNumber, ShipmentStatus.Preparing);
        }
        else
        {
            (id, tracking, status) = (existing.Id, existing.TrackingNumber, existing.Status);
        }

        bool reachable = !request.ShippingAddress.Contains("fail", StringComparison.OrdinalIgnoreCase);
        if (status == ShipmentStatus.Preparing)
        {
            await MoveAsync(id, request.ShippingAddress, ShipmentStatus.InTransit, cancellationToken).ConfigureAwait(false);
            status = ShipmentStatus.InTransit;
        }

        if (status == ShipmentStatus.InTransit)
        {
            status = reachable ? ShipmentStatus.Delivered : ShipmentStatus.Returned;
            await MoveAsync(id, request.ShippingAddress, status, cancellationToken).ConfigureAwait(false);
        }

        bool delivered = status == ShipmentStatus.Delivered;
        ShipmentCompletedEto completed = new(request.OrderId, id, tracking, delivered, delivered ? null : "the carrier could not reach the address");
        await eventBus.PublishAsync(completed, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Task MoveAsync(Guid id, string address, ShipmentStatus status, CancellationToken cancellationToken)
        => shipmentService.UpdateAsync(new ShipmentUpdateDto { Id = id, Carrier = Carrier, Address = address, Status = status.ToString() }, cancellationToken);
}
