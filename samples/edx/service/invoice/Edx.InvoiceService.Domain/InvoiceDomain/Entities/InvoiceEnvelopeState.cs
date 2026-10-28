namespace Edx.InvoiceService.Domain.InvoiceDomain.Entities;

/// <summary>
/// Where the envelope of an invoice is, as the GİB invoice integration tells it. It is a summary kept on the invoice (the envelope itself, with its history, is the integration's); it only goes forward. The invoice's own status ends when it is signed, the envelope's ends
/// when it is placed at the receiver's system or fails.
/// </summary>
public enum InvoiceEnvelopeState
{
    /// <summary>The invoice has no envelope yet (it is not signed).</summary>
    None = 0,

    /// <summary>The invoice is signed and waits to be put in an envelope and handed to the center.</summary>
    ToSend = 1,

    /// <summary>The center took the envelope.</summary>
    Sent = 2,

    /// <summary>The envelope was placed at the receiver's system (1300).</summary>
    Delivered = 3,

    /// <summary>The center refused the envelope (a code of 1100 to 1199); it is not sent again.</summary>
    Refused = 4,

    /// <summary>The envelope did not reach the receiver (the center could not be reached, or could not deliver it, or the receiver's answer failed).</summary>
    Failed = 5,
}
