namespace Edx.InvoiceService.Domain.InvoiceDomain.Entities;

/// <summary>One step in the life of an invoice (kept inside the invoice): its status changed, or its envelope did.</summary>
public sealed class InvoiceHistoryEntry
{
    // for the database driver
    private InvoiceHistoryEntry()
    {
    }

    /// <summary><c>Status</c> or <c>Envelope</c>.</summary>
    public string Kind { get; private set; } = string.Empty;

    /// <summary>The status (or the envelope state) it moved to.</summary>
    public string Value { get; private set; } = string.Empty;

    public DateTimeOffset At { get; private set; }

    /// <summary>Who did it, when a person did (the approver, the rejecter).</summary>
    public Guid? By { get; private set; }

    /// <summary>The reason or the code and text the center gave.</summary>
    public string? Note { get; private set; }

    public static InvoiceHistoryEntry Of(string kind, string value, DateTimeOffset at, Guid? by = null, string? note = null)
        => new() { Kind = kind, Value = value, At = at, By = by, Note = note };
}
