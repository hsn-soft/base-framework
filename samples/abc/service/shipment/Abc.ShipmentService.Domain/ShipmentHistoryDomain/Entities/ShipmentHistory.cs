using Abc.ShipmentService.Domain.ShipmentHistoryDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Abc.ShipmentService.Domain.ShipmentHistoryDomain.Entities;

/// <summary>
/// How a delivery ended, kept in the shipment history database (EF Core, PostgreSQL; the shipments are in MongoDB): delivered, or failed. Who recorded it and when come from <see cref="FullAuditedEntity{TKey}"/> (the soft delete too).
/// The shipment is only referenced by its id and its tracking number: the history is in another database than the shipments.
/// </summary>
public sealed class ShipmentHistory : FullAuditedEntity<Guid>, IMultiTenant
{
    // for EF Core
    private ShipmentHistory()
    {
    }

    public Guid ShipmentId { get; private set; }

    public Guid OrderId { get; private set; }

    public string TrackingNumber { get; private set; } = string.Empty;

    /// <summary><see cref="ShipmentHistoryConsts.Delivered"/>, <see cref="ShipmentHistoryConsts.Failed"/> or any other word of at most <see cref="ShipmentHistoryConsts.OutcomeMaxLength"/> characters, in small letters.</summary>
    public string Outcome { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public Guid TenantId { get; private set; }

    public static ShipmentHistory Record(Guid shipmentId, Guid orderId, string trackingNumber, string outcome, string? note = null)
    {
        ShipmentHistory history = new()
        {
            Id = Guid.CreateVersion7(),
            ShipmentId = Guard.NotDefault<Guid>(shipmentId, nameof(ShipmentId)),
            OrderId = Guard.NotDefault<Guid>(orderId, nameof(OrderId)),
            TrackingNumber = Guard.NotNullOrWhiteSpace(trackingNumber, nameof(TrackingNumber), ShipmentHistoryConsts.TrackingNumberMaxLength).Trim().ToUpperInvariant(),
        };
        history.Describe(outcome, note);
        return history;
    }

    /// <summary>Changes the outcome and the note; which parcel it was is a fact and stays.</summary>
    public void Update(string outcome, string? note) => Describe(outcome, note);

    private void Describe(string outcome, string? note)
    {
        Outcome = Guard.NotNullOrWhiteSpace(outcome, nameof(Outcome), ShipmentHistoryConsts.OutcomeMaxLength).Trim().ToLowerInvariant();
        Note = string.IsNullOrWhiteSpace(note) ? null : Guard.Length(note.Trim(), nameof(Note), ShipmentHistoryConsts.NoteMaxLength);
    }
}
