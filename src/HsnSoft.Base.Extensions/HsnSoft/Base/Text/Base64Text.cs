using System;
using System.Buffers.Text;
using System.Text;

namespace HsnSoft.Base.Text;

/// <summary>Text to Base64 and back, as UTF-8: the standard alphabet (<see cref="Encode"/>) and the URL-safe one (<see cref="EncodeUrl"/>: <c>-</c> and <c>_</c>, no padding), which is safe in an address, a header or a file name.</summary>
public static class Base64Text
{
    /// <summary>The standard Base64 of the UTF-8 bytes of <paramref name="text"/>.</summary>
    public static string Encode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>The text of a standard Base64 value; a value that is not Base64 is a <see cref="FormatException"/>.</summary>
    public static string Decode(string base64)
    {
        ArgumentNullException.ThrowIfNull(base64);

        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    /// <summary>Whether <paramref name="base64"/> was a standard Base64 value of UTF-8 text, and the text.</summary>
    public static bool TryDecode(string? base64, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrEmpty(base64))
        {
            return false;
        }

        try
        {
            text = Decode(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>The URL-safe Base64 (no padding) of the UTF-8 bytes of <paramref name="text"/>.</summary>
    public static string EncodeUrl(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>The text of a URL-safe Base64 value; a value that is not one is a <see cref="FormatException"/>.</summary>
    public static string DecodeUrl(string base64Url)
    {
        ArgumentNullException.ThrowIfNull(base64Url);

        return Encoding.UTF8.GetString(Base64Url.DecodeFromChars(base64Url));
    }

    /// <summary>Whether <paramref name="base64Url"/> was a URL-safe Base64 value of UTF-8 text, and the text.</summary>
    public static bool TryDecodeUrl(string? base64Url, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrEmpty(base64Url))
        {
            return false;
        }

        try
        {
            text = DecodeUrl(base64Url);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
