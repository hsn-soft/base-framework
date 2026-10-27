using System.Globalization;

namespace Edx.IntakeService.Application.Storage;

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

    /// <summary>The name of an object in the store: a part of the id keeps two files of the same name in the same minute apart.</summary>
    public static string ObjectFileName(Guid id, string cleanName) => $"{id:N}_{cleanName}";

    /// <summary>The folders of a time, from the year down: <c>2026</c>, <c>10</c>, <c>27</c>, <c>14</c>, <c>35</c> (the time in UTC).</summary>
    public static string[] TimeSegments(DateTimeOffset time)
    {
        DateTimeOffset utc = time.ToUniversalTime();
        CultureInfo culture = CultureInfo.InvariantCulture;
        return [utc.ToString("yyyy", culture), utc.ToString("MM", culture), utc.ToString("dd", culture), utc.ToString("HH", culture), utc.ToString("mm", culture)];
    }
}
