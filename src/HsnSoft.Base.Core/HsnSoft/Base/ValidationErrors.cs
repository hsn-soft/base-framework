using System;
using System.Collections.Generic;
using System.Linq;

namespace HsnSoft.Base;

/// <summary>
/// One thing that is wrong with a request: the field, what to say in English (what the log says) and, for the client in its own language, the key of the text and its arguments (the same <c>WithKey</c> of the
/// other errors: <c>{0}</c>, <c>{1}</c> are the arguments).
/// </summary>
public sealed record ValidationError(string Field, string Message, string? Key = null, IReadOnlyList<object?>? Args = null);

/// <summary>
/// Collects what is wrong, so the client is told all of it at once and not one thing at a time: <c>errors.Add("Sku", "The sku is taken.", "product_sku_duplicate", sku); errors.ThrowIfAny();</c>.
/// For the rules of the business that an attribute cannot say (a value that is taken, a limit of the account, a field that depends on another), asked in the application service.
/// </summary>
public sealed class ValidationErrors
{
    private readonly List<ValidationError> _errors = [];

    public bool Any => _errors.Count > 0;

    public IReadOnlyList<ValidationError> Items => _errors;

    public ValidationErrors Add(string field, string message, string? key = null, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _errors.Add(new ValidationError(field, message, key, args));
        return this;
    }

    /// <summary>Adds the error when <paramref name="isWrong"/> is true.</summary>
    public ValidationErrors AddIf(bool isWrong, string field, string message, string? key = null, params object?[] args)
        => isWrong ? Add(field, message, key, args) : this;

    /// <exception cref="ValidationFailedException">Something was wrong: the answer is 400 with all of it.</exception>
    public void ThrowIfAny()
    {
        if (Any)
        {
            throw new ValidationFailedException(_errors);
        }
    }
}

/// <summary>A request that has things wrong with it, all of them: answered as 400, <c>validation_error</c>, with the list of the fields and a message for each in the language of the request.</summary>
public sealed class ValidationFailedException(IReadOnlyList<ValidationError> errors) : Exception(Describe(errors))
{
    public IReadOnlyList<ValidationError> Errors { get; } = errors;

    private static string Describe(IReadOnlyList<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return errors.Count == 0 ? "Validation failed." : "Validation failed: " + string.Join("; ", errors.Select(error => $"{error.Field}: {error.Message}"));
    }
}
