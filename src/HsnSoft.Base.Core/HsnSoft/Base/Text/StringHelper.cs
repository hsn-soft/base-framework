using System.Globalization;
using System.Linq;
using System.Text;

namespace HsnSoft.Base.Text;

public static class StringHelper
{
    /// <summary>
    /// The text as it is compared and searched: trimmed, in capitals, without the marks of the letters (<c>Müller</c> and <c>MULLER</c> are the same; <see langword="null"/> is empty). Entities keep it next to the text
    /// (<c>NormalizedName</c>) so a list can look for part of it without regard to case.
    /// </summary>
    public static string Normalize(string? value) => Strip((value ?? string.Empty).Trim().ToUpperInvariant());

    /// <summary>The same in small letters (a key, a slug).</summary>
    public static string Minimize(string? value) => Strip((value ?? string.Empty).Trim().ToLowerInvariant());

    private static string Strip(string text)
        => string.Concat(text.Normalize(NormalizationForm.FormD).Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
}
