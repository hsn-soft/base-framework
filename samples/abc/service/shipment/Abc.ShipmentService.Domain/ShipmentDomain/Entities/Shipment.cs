using Abc.ShipmentService.Domain.ShipmentDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Abc.ShipmentService.Domain.ShipmentDomain.Entities;

/// <summary>
/// A parcel on its way to the customer of an order.
/// The audit fields, the soft delete and the tenant are done by the persistence (<see cref="ICreationAudited"/>, <see cref="IModificationAudited"/>, <see cref="ISoftDelete"/>,
/// <see cref="IMultiTenant"/>): the entity only keeps them.
/// </summary>
public sealed class Shipment : ICreationAudited, IModificationAudited, ISoftDelete, IMultiTenant
{
    // for the database driver
    private Shipment()
    {
    }

    public Guid Id { get; private set; }

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

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);

    public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);

    public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);

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

    /// <summary>A carrier or a tracking number as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

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
        Carrier = Required(carrier, ShipmentConsts.CarrierMaxLength, nameof(Carrier));
        NormalizedCarrier = Normalize(Carrier);
        Address = Required(address, ShipmentConsts.AddressMaxLength, nameof(Address));
    }

    private static string Required(string value, int maxLength, string name)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length == 0 || text.Length > maxLength
            ? throw new DomainException($"{name} is required and at most {maxLength} characters.").WithKey("field_required", name, maxLength)
            : text;
    }
}
