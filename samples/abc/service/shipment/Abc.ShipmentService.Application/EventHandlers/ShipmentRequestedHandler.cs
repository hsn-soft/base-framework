using Abc.Shared.Events;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;

namespace Abc.ShipmentService.Application.EventHandlers;

/// <summary>
/// The stock of an order is set aside: makes the shipment, sends it (the carrier is simulated: the parcel is delivered, unless the address says <c>fail</c>, which the carrier cannot reach), and tells the order service how it ended
/// (<see cref="ShipmentCompletedEto"/>). The shipment and its history are made by the application service, as for a request. A message that comes twice finds the shipment of the order and finishes what was left.
/// </summary>
public sealed class ShipmentRequestedHandler(IShipmentAppService shipmentService, IRepository<Shipment, Guid> shipments, IEventBus eventBus, ICurrentTenant tenant) : IIntegrationEventHandler<ShipmentRequestedEto>
{
    private const string Carrier = "Abc Express";

    public async Task HandleAsync(MessageEnvelope<ShipmentRequestedEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        ShipmentRequestedEto request = envelope.Message;
        ShipmentCompletedEto completed;
        using (tenant.Change(request.TenantId))
        {
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
            completed = new ShipmentCompletedEto(request.OrderId, request.TenantId, id, tracking, delivered, delivered ? null : "the carrier could not reach the address");
        }

        await eventBus.PublishAsync(completed, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Task MoveAsync(Guid id, string address, ShipmentStatus status, CancellationToken cancellationToken)
        => shipmentService.UpdateAsync(new ShipmentUpdateDto { Id = id, Carrier = Carrier, Address = address, Status = status.ToString() }, cancellationToken);
}
