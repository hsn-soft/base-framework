using Abc.ShipmentService.Domain.ShipmentDomain.Consts;
using HsnSoft.Base;

namespace Abc.ShipmentService.Domain.ShipmentDomain.Entities;

/// <summary>A parcel on its way to the customer of an order.</summary>
public sealed class Shipment
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

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The user name in the <c>sub</c> claim of the token of who made the shipment; <see langword="null"/> when nobody signed in did.</summary>
    public string? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The same for the last change.</summary>
    public string? UpdatedBy { get; private set; }

    public static Shipment Create(Guid orderId, string carrier, string address, DateTimeOffset now, string? by)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("A shipment needs an order.");
        }

        Shipment shipment = new()
        {
            Id = Guid.CreateVersion7(),
            OrderId = orderId,
            TrackingNumber = $"SHP-{Guid.NewGuid():N}"[..14].ToUpperInvariant(),
            Status = ShipmentStatus.Preparing,
            CreatedAt = now,
            CreatedBy = Author(by),
        };
        shipment.Change(carrier, address, now, by);
        return shipment;
    }

    /// <summary>A carrier or a tracking number as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

    /// <summary>Changes the carrier, the address and the status; a delivered or returned shipment is final.</summary>
    public void Update(string carrier, string address, ShipmentStatus status, DateTimeOffset now, string? by)
    {
        if (Status is ShipmentStatus.Delivered or ShipmentStatus.Returned && status != Status)
        {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} shipment cannot change its status.");
        }

        Change(carrier, address, now, by);
        Status = status;
    }

    private void Change(string carrier, string address, DateTimeOffset now, string? by)
    {
        Carrier = Required(carrier, ShipmentConsts.CarrierMaxLength, nameof(Carrier));
        NormalizedCarrier = Normalize(Carrier);
        Address = Required(address, ShipmentConsts.AddressMaxLength, nameof(Address));
        UpdatedAt = now;
        UpdatedBy = Author(by);
    }

    private static string? Author(string? by) => string.IsNullOrWhiteSpace(by) ? null : by.Trim()[..Math.Min(by.Trim().Length, ShipmentConsts.AuthorMaxLength)];

    private static string Required(string value, int maxLength, string name)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length == 0 || text.Length > maxLength
            ? throw new DomainException($"{name} is required and at most {maxLength} characters.")
            : text;
    }
}
