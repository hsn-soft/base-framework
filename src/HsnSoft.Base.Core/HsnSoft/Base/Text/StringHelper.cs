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

    /// <summary>
    /// The text with every character that is not a letter A-Z, a digit, <c>-</c>, <c>.</c> or <c>_</c> replaced by <paramref name="replacement"/> (nothing, by default); <c>+</c> and <c>@</c> are kept when <paramref name="isEmail"/> is
    /// <see langword="true"/>. Only the ASCII letters stay: a letter with a mark (<c>ş</c>) or of another alphabet is replaced, so call <see cref="Minimize"/> first when it should become <c>s</c>. <see langword="null"/> and blank are empty.
    /// </summary>
    public static string ReplaceInvalidChars(string? text, bool isEmail = false, string replacement = "")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        StringBuilder result = new(text.Length);
        foreach (char character in text)
        {
            bool valid = character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '.' or '_'
                || (isEmail && character is '+' or '@');
            if (valid)
            {
                result.Append(character);
            }
            else
            {
                result.Append(replacement);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// A key made of a name that is safe in a address, a file or a setting: in small letters, the marks of the letters taken off (<c>Çanakkale</c> becomes <c>canakkale</c>; the dotless <c>ı</c> and a few letters that do not decompose
    /// (<c>ł</c>, <c>ø</c>, <c>đ</c>) are mapped to their usual ASCII letter), every other character replaced by <paramref name="separator"/> and the separator trimmed from both ends (<c>"Acme Corp."</c> becomes <c>acme_corp.</c>).
    /// </summary>
    public static string SlugKeyNormalize(string? value, char separator = '_')
    {
        string minimized = Minimize(value).Replace('ı', 'i').Replace('ł', 'l').Replace('ø', 'o').Replace('đ', 'd');
        return ReplaceInvalidChars(minimized, false, separator.ToString()).Trim(separator);
    }

    private static string Strip(string text)
        => string.Concat(text.Normalize(NormalizationForm.FormD).Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
}
