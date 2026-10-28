namespace Edx.InvoiceService.Domain.InvoiceDomain.Entities;

/// <summary>Where an invoice is on its way from the queue to its signed XML.</summary>
public enum SalesInvoiceStatus
{
    /// <summary>Taken from the queue: it is being fetched, unpacked and checked.</summary>
    Processing = 1,

    /// <summary>It could not be taken in (see the stage and the reason). It stays so, visible, until a correct one of the same invoice comes and replaces it.</summary>
    Failed = 2,

    /// <summary>The draft XML was made and waits for a person to approve or reject it.</summary>
    DraftCreated = 3,

    /// <summary>A person rejected the draft. It stays, visible, until a correct one of the same invoice comes; the same number may be sent again.</summary>
    Rejected = 4,

    /// <summary>A person approved the draft: it is being signed.</summary>
    Approved = 5,

    // the word of the domain: a CA1720 hint that it is a type name does not apply to a status
#pragma warning disable CA1720
    /// <summary>Signed and kept in its signed place: its envelope is the GİB invoice integration's business (see InvoiceEnvelopeState): the invoice's own life ends here.</summary>
    Signed = 6,
#pragma warning restore CA1720
}
