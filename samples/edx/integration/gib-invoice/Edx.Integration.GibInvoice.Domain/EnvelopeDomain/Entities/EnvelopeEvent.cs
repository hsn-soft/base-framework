namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;

/// <summary>One thing that happened to an envelope, kept inside it: a code of the center (or of this service) with its text, when, and who said it.</summary>
public sealed class EnvelopeEvent
{
    public const string SourceLocal = "Local";
    public const string SourceSend = "Send";
    public const string SourceCenter = "Center";
    public const string SourcePoll = "Poll";
    public const string SourcePostBox = "PostBox";

    // for the database driver
    private EnvelopeEvent()
    {
    }

    public DateTimeOffset At { get; private set; }

    /// <summary>The code of Ek-2 (1000, 1200, 1220, 1300...), or 0 when the event has none.</summary>
    public int Code { get; private set; }

    public string Text { get; private set; } = string.Empty;

    /// <summary>Who said it: <c>Local</c> (this service), <c>Send</c> (the answer of <c>sendDocument</c>), <c>Center</c> (a system envelope), <c>Poll</c> (the answer of <c>getApplicationResponse</c>), <c>PostBox</c> (this post box).</summary>
    public string Source { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public static EnvelopeEvent Of(DateTimeOffset at, int code, string text, string source, string? note = null)
        => new() { At = at, Code = code, Text = text, Source = source, Note = note };
}
