using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace HsnSoft.Base;

/// <summary>
/// For the rules of a request that an attribute cannot say (<c>IValidatableObject.Validate</c>): a result that names a text instead of carrying one, so the client reads it in its language.
/// <c>yield return ValidationRule.Failed(nameof(Email), "email_taken", Email)</c>; the web api translates it with the arguments, as it does the attributes. The name of the field in an argument is replaced by its label.
/// </summary>
public static class ValidationRule
{
    private const char Marker = '\u001f';

    public static ValidationResult Failed(string memberName, string key, params object?[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return new ValidationResult(Encode(key, args), [memberName]);
    }

    /// <summary>The field is required (<c>Validation:Required</c>).</summary>
    public static ValidationResult Required(string memberName) => Failed(memberName, "Validation:Required", memberName);

    /// <summary>The text is longer than <paramref name="maxLength"/> (<c>Validation:MaxLength</c>).</summary>
    public static ValidationResult TooLong(string memberName, int maxLength) => Failed(memberName, "Validation:MaxLength", memberName, maxLength);

    /// <summary>The value is not a valid one (<c>Validation:InvalidValue</c>).</summary>
    public static ValidationResult Invalid(string memberName) => Failed(memberName, "Validation:InvalidValue", memberName);

    /// <summary>The message a <see cref="ValidationResult"/> carries for a key and its arguments (what <see cref="TryDecode"/> reads back).</summary>
    public static string Encode(string key, params object?[] args)
        => Marker + key + string.Concat(args.Select(argument => Marker + Convert.ToString(argument, CultureInfo.InvariantCulture)));

    /// <summary>Whether <paramref name="message"/> was made by <see cref="Encode"/>, and the key and the arguments in it.</summary>
    public static bool TryDecode(string? message, out string key, out string[] args)
    {
        key = string.Empty;
        args = [];
        if (string.IsNullOrEmpty(message) || message[0] != Marker)
        {
            return false;
        }

        string[] parts = message[1..].Split(Marker);
        key = parts[0];
        args = parts[1..];
        return key.Length > 0;
    }
}
