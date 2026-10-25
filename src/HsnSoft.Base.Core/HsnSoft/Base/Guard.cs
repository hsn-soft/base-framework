using System;
using System.Collections;
using System.Numerics;

namespace HsnSoft.Base;

/// <summary>
/// Checks a value and fails with a <see cref="DomainException"/> that names a text (<c>Validation:*</c>), so the client reads the message in its language and the code that checks needs no localizer. The
/// message of the exception is English (what the log says); the first argument of the text is the name of the parameter. The methods return the value, so a check can be part of an assignment.
/// </summary>
public static class Guard
{
    /// <summary>The value is not <see langword="null"/>.</summary>
    public static T NotNull<T>(T? value, string parameterName)
        where T : class
        => value ?? throw new DomainException($"{parameterName} is required.").WithKey("Validation:Required", parameterName);

    /// <summary>The value is not <see langword="null"/> and not the default of its type (an empty <see cref="Guid"/>, 0).</summary>
    public static T NotDefault<T>(T? value, string parameterName)
        where T : struct
        => value is { } present && !present.Equals(default(T)) ? present : throw new DomainException($"{parameterName} is required.").WithKey("Validation:Required", parameterName);

    /// <summary>The collection has at least one item.</summary>
    public static TCollection NotNullOrEmpty<TCollection>(TCollection? value, string parameterName)
        where TCollection : class, ICollection
        => value is { Count: > 0 } ? value : throw new DomainException($"{parameterName} must not be empty.").WithKey("Validation:IsNotEmpty", parameterName);

    /// <summary>The text is not <see langword="null"/>, empty or white space; it is returned trimmed, and checked for its length.</summary>
    public static string NotNullOrWhiteSpace(string? value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{parameterName} is required.").WithKey("Validation:Required", parameterName);
        }

        return Length(value.Trim(), parameterName, maxLength, minLength)!;
    }

    /// <summary>The text is at most <paramref name="maxLength"/> and at least <paramref name="minLength"/> characters (<see langword="null"/> counts as empty and is allowed when the minimum is 0).</summary>
    public static string? Length(string? value, string parameterName, int maxLength, int minLength = 0)
    {
        int length = value?.Length ?? 0;
        if (length > maxLength)
        {
            throw new DomainException($"{parameterName} is at most {maxLength} characters.").WithKey("Validation:MaxLength", parameterName, maxLength);
        }

        return length < minLength
            ? throw new DomainException($"{parameterName} is at least {minLength} characters.").WithKey("Validation:MinLength", parameterName, minLength)
            : value;
    }

    /// <summary>The number is between <paramref name="minimum"/> and <paramref name="maximum"/>, both allowed.</summary>
    public static T Range<T>(T value, string parameterName, T minimum, T maximum)
        where T : INumber<T>
        => value >= minimum && value <= maximum
            ? value
            : throw new DomainException($"{parameterName} must be between {minimum} and {maximum}.").WithKey("Validation:Range", parameterName, minimum, maximum);

    /// <summary>The number is not below 0.</summary>
    public static T NotNegative<T>(T value, string parameterName)
        where T : INumber<T>
        => value >= T.Zero ? value : throw new DomainException($"{parameterName} cannot be negative.").WithKey("Validation:NotNegative", parameterName);
}
