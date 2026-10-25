using System;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base;

/// <summary>
/// A text that is given is not empty and not only white space. A missing text (<see langword="null"/>) is valid: use it on an optional text, where <see cref="RequiredAttribute"/> would refuse the missing one too.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute() : ValidationAttribute("The {0} field must not be empty or white space.")
{
    public override bool IsValid(object? value) => value is null || (value is string text && !string.IsNullOrWhiteSpace(text));
}
