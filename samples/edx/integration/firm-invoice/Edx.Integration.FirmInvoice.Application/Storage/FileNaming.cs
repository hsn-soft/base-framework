using System.Globalization;

namespace Edx.Integration.FirmInvoice.Application.Storage;

/// <summary>The names the service gives: what a customer's file name becomes in the store, and the segments of a time's folders.</summary>
public static class FileNaming
{
    /// <summary>The name a customer gave without any path in it and without characters a store may not take; <c>file</c> when nothing is left.</summary>
    public static string Clean(string? fileName)
    {
        string name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
        char[] invalid = [.. Path.GetInvalidFileNameChars(), ' '];
        string cleaned = string.Concat(name.Select(character => invalid.Contains(character) || char.IsControl(character) ? '_' : character)).Trim('.', '_');
        return cleaned.Length == 0 ? "file" : cleaned.Length > 120 ? cleaned[^120..] : cleaned;
    }

    /// <summary>The zip that came in is kept as <c>{queueId}_invoice.zip</c> whatever the customer called it (the name they gave is on the queue item).</summary>
    public static string ObjectFileName(Guid id) => $"{id:N}_invoice.zip";

    /// <summary>The answer to that invoice (when answers are made) is kept beside it as <c>{queueId}_invoice_answer.zip</c>.</summary>
    public static string AnswerFileName(Guid id) => $"{id:N}_invoice_answer.zip";

    /// <summary>The folders of a time: <c>2026</c>, <c>1027</c> (month and day), <c>1435</c> (hour and minute), the time in UTC.</summary>
    public static string[] TimeSegments(DateTimeOffset time)
    {
        DateTimeOffset utc = time.ToUniversalTime();
        CultureInfo culture = CultureInfo.InvariantCulture;
        return [utc.ToString("yyyy", culture), utc.ToString("MMdd", culture), utc.ToString("HHmm", culture)];
    }
}
