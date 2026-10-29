namespace Edx.Integration.FirmInvoice.Domain.QueueDomain.Entities;

/// <summary>Where an invoice of the queue is. The firm invoice integration sets <see cref="Queued"/>; the others are what the invoice service said happened to it (events).</summary>
public enum QueueStatus
{
    /// <summary>Checked and kept: it waits for the invoice service to take it in (or is being taken in).</summary>
    Queued = 1,

    /// <summary>The draft was made: it waits for a person to approve or reject it.</summary>
    DraftCreated = 2,

    /// <summary>A person rejected the draft. The sender may send the invoice again with the same number.</summary>
    Rejected = 3,

    /// <summary>The invoice could not be taken in (see the reason); it stays so until a correct one of the same invoice comes, which replaces it.</summary>
    Failed = 4,

    /// <summary>Approved, signed and kept: nothing is left to do.</summary>
    Completed = 5,
}
