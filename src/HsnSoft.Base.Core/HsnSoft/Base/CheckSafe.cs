using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base;

public static class CheckSafe
{
    public static bool NotNull<T>(T value, [InvokerParameterName] [NotNull] string parameterName)
    {
        try
        {
            Check.NotNull(value, parameterName);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool NotNull<T>(T value, [InvokerParameterName] [NotNull] string parameterName, string message)
    {
        try
        {
            Check.NotNull(value, parameterName, message);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool NotNull(string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        try
        {
            Check.NotNull(value, parameterName, maxLength, minLength);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool NotNullOrWhiteSpace(string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        try
        {
            Check.NotNullOrWhiteSpace(value, parameterName, maxLength, minLength);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool NotNullOrEmpty(string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        try
        {
            Check.NotNullOrEmpty(value, parameterName, maxLength, minLength);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool NotNullOrEmpty<T>(ICollection<T> value, [InvokerParameterName] [NotNull] string parameterName)
    {
        try
        {
            Check.NotNullOrEmpty(value, parameterName);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool Length([CanBeNull] string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength, int minLength = 0)
    {
        try
        {
            Check.Length(value, parameterName, maxLength, minLength);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool Range(short value, [InvokerParameterName] [NotNull] string parameterName, short minimumValue, short maximumValue = short.MaxValue)
    {
        try
        {
            Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool Range(int value, [InvokerParameterName] [NotNull] string parameterName, int minimumValue, int maximumValue = int.MaxValue)
    {
        try
        {
            Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool Range(long value, [InvokerParameterName] [NotNull] string parameterName, long minimumValue, long maximumValue = long.MaxValue)
    {
        try
        {
            Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool Range(float value, [InvokerParameterName] [NotNull] string parameterName, float minimumValue, float maximumValue = float.MaxValue)
    {
        try
        {
            Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool Range(double value, [InvokerParameterName] [NotNull] string parameterName, double minimumValue, double maximumValue = double.MaxValue)
    {
        try
        {
            Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool Range(decimal value, [InvokerParameterName] [NotNull] string parameterName, decimal minimumValue, decimal maximumValue = decimal.MaxValue)
    {
        try
        {
            Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception) { return false; }

        return true;
    }

    public static bool NotDefaultOrNull<T>(T? value, [InvokerParameterName] [NotNull] string parameterName)
        where T : struct
    {
        try
        {
            Check.NotDefaultOrNull(value, parameterName);
        }
        catch (Exception) { return false; }

        return true;
    }
}