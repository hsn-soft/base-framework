using System;

namespace HsnSoft.Base;

/// <summary>
/// Basic arithmetic on <see cref="decimal"/> values, so results are exact (no binary floating point rounding).
/// </summary>
public static class Calculator
{
    /// <summary>Returns <paramref name="left"/> + <paramref name="right"/>.</summary>
    /// <exception cref="OverflowException">The result does not fit in a <see cref="decimal"/>.</exception>
    public static decimal Add(decimal left, decimal right) => left + right;

    /// <summary>Returns <paramref name="left"/> - <paramref name="right"/>.</summary>
    /// <exception cref="OverflowException">The result does not fit in a <see cref="decimal"/>.</exception>
    public static decimal Subtract(decimal left, decimal right) => left - right;

    /// <summary>Returns <paramref name="left"/> * <paramref name="right"/>.</summary>
    /// <exception cref="OverflowException">The result does not fit in a <see cref="decimal"/>.</exception>
    public static decimal Multiply(decimal left, decimal right) => left * right;

    /// <summary>Returns <paramref name="dividend"/> / <paramref name="divisor"/>.</summary>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> is zero.</exception>
    public static decimal Divide(decimal dividend, decimal divisor)
    {
        if (divisor == 0m)
        {
            throw new DivideByZeroException("The divisor must not be zero.");
        }

        return dividend / divisor;
    }
}
