using Abc.ShipmentService.Domain.ShipmentDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Abc.ShipmentService.Domain.ShipmentDomain.Entities;

/// <summary>
/// A parcel on its way to the customer of an order.
/// The audit fields and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class Shipment : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private Shipment()
    {
    }

    /// <summary>The order this shipment carries (the order service's; this service keeps only the id).</summary>
    public Guid OrderId { get; private set; }

    /// <summary>What the customer follows the parcel by: made when the shipment is, and never changed.</summary>
    public string TrackingNumber { get; private set; } = string.Empty;

    public string Carrier { get; private set; } = string.Empty;

    /// <summary>The carrier in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedCarrier { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public ShipmentStatus Status { get; private set; }

    public Guid TenantId { get; private set; }

    public static Shipment Create(Guid orderId, string carrier, string address)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("A shipment needs an order.").WithKey("shipment_order_required");
        }

        Shipment shipment = new()
        {
            Id = Guid.CreateVersion7(),
            OrderId = orderId,
            TrackingNumber = $"SHP-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            Status = ShipmentStatus.Preparing,
        };
        shipment.Change(carrier, address);
        return shipment;
    }

    /// <summary>Changes the carrier, the address and the status; a delivered or returned shipment is final.</summary>
    public void Update(string carrier, string address, ShipmentStatus status)
    {
        if (Status is ShipmentStatus.Delivered or ShipmentStatus.Returned && status != Status)
        {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} shipment cannot change its status.").WithKey("shipment_status_final", Status.ToString());
        }

        Change(carrier, address);
        Status = status;
    }

    private void Change(string carrier, string address)
    {
        Carrier = Guard.NotNullOrWhiteSpace(carrier, nameof(Carrier), ShipmentConsts.CarrierMaxLength);
        NormalizedCarrier = StringHelper.Normalize(Carrier);
        Address = Guard.NotNullOrWhiteSpace(address, nameof(Address), ShipmentConsts.AddressMaxLength);
    }
}
