using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base;

/// <summary>
/// No item of a list of texts is empty, only white space or missing (<c>["admin", ""]</c>). A missing list is valid, and so is an empty one (<see cref="MinLengthAttribute"/> asks for at least one item).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NoBlankItemsAttribute() : ValidationAttribute("The {0} field must not have an empty item.")
{
    public override bool IsValid(object? value)
    {
        if (value is string || value is not IEnumerable items)
        {
            return true;
        }

        foreach (object? item in items)
        {
            if (item is not string text || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
        }

        return true;
    }
}
