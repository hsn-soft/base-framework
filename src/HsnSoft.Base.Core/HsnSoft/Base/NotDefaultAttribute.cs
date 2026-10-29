using System;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base;

/// <summary>
/// The value is not the default of its type: not <see cref="Guid.Empty"/>, not <c>0</c>, not the first value of an enum that nobody chose. A missing value (<see langword="null"/>) is not its business: that is <see cref="RequiredAttribute"/>'s.
/// Declare the property nullable with <c>[Required]</c> and <c>[NotDefault]</c> (<c>Guid?</c>), so "missing" and "the empty guid" are both refused, each with its own text.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotDefaultAttribute() : ValidationAttribute("The {0} field must not be the default value.")
{
    public override bool IsValid(object? value)
        => value is null || !Equals(value, Activator.CreateInstance(value.GetType()));
}
