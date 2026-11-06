using System.Globalization;
using System.Linq;
using System.Text;

namespace HsnSoft.Base.Text;

public static class StringHelper
{
    /// <summary>
    /// The text as it is compared and searched, made of nothing but <c>A-Z</c>, <c>0-9</c> and <c>@ . - _ +</c>: the letters are changed to their English letters first (<c>ı</c> and <c>İ</c> become <c>i</c> and <c>I</c>; <c>öçşğü</c> and <c>ÖÇŞĞÜ</c> become
    /// <c>ocsgu</c> and <c>OCSGU</c>; any other mark of a letter is taken off, <c>Müller</c> is <c>MULLER</c>), then everything is in capitals and every character that is not one of those allowed (a space, a punctuation mark, a letter of another alphabet) is left out:
    /// <c>"Tech Summus A.Ş."</c> is <c>TECHSUMMUSA.S.</c>. <see langword="null"/> and blank are empty. Entities keep it next to the text (<c>NormalizedName</c>) so a list can look for part of it without regard to case or spelling.
    /// </summary>
    public static string Normalize(string? value)
    {
        string text = Strip(Transliterate(value ?? string.Empty)).ToUpperInvariant();
        StringBuilder result = new(text.Length);
        foreach (char character in text)
        {
            if (character is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '@' or '.' or '-' or '_' or '+')
            {
                result.Append(character);
            }
        }

        return result.ToString();
    }

    /// <summary>The text in small letters with the letters changed to English ones (<c>ı</c> is <c>i</c>) and the marks taken off, trimmed; the other characters stay (a key, a slug).</summary>
    public static string Minimize(string? value) => Strip(Transliterate((value ?? string.Empty).Trim())).ToLowerInvariant();

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
    /// A key made of a name that is safe in a address, a file or a setting: in small letters, the marks of the letters taken off (<c>Çanakkale</c> becomes <c>canakkale</c>; the dotless <c>ı</c> and the letters that do not decompose are mapped as in <see cref="Minimize"/>), every other character replaced by <paramref name="separator"/> and the separator trimmed from both ends (<c>"Acme Corp."</c> becomes <c>acme_corp.</c>).
    /// </summary>
    public static string SlugKeyNormalize(string? value, char separator = '_')
    {
        return ReplaceInvalidChars(Minimize(value), false, separator.ToString()).Trim(separator);
    }

    /// <summary>
    /// The Turkish letters written out, changed to their English letters before the case is changed (<c>ı İ ö Ö ç Ç ş Ş ğ Ğ ü Ü</c>; <c>ı</c> and <c>İ</c> have no decomposition into a letter and a mark and a culture changes them to the wrong case), and the other letters
    /// that have no decomposition (<c>ł ø đ ß</c>), so <see cref="Strip"/> only has the remaining marks (<c>é</c>, <c>ñ</c>) to take off.
    /// </summary>
    private static string Transliterate(string text)
    {
        StringBuilder result = new(text.Length);
        foreach (char character in text)
        {
            result.Append(character switch
            {
                'ı' => "i",
                'İ' => "I",
                'ö' => "o",
                'Ö' => "O",
                'ç' => "c",
                'Ç' => "C",
                'ş' => "s",
                'Ş' => "S",
                'ğ' => "g",
                'Ğ' => "G",
                'ü' => "u",
                'Ü' => "U",
                'ł' => "l",
                'Ł' => "L",
                'ø' => "o",
                'Ø' => "O",
                'đ' => "d",
                'Đ' => "D",
                'ß' => "ss",
                _ => character.ToString(),
            });
        }

        return result.ToString();
    }

    private static string Strip(string text)
        => string.Concat(text.Normalize(NormalizationForm.FormD).Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
}
