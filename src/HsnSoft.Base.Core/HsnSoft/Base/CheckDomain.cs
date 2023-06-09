using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base;

public static class CheckDomain
{
    public static T NotNull<T>(T value, [InvokerParameterName] [NotNull] string parameterName)
    {
        try
        {
            return Check.NotNull(value, parameterName);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static T NotNull<T>(T value, [InvokerParameterName] [NotNull] string parameterName, string message)
    {
        try
        {
            return Check.NotNull(value, parameterName, message);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static string NotNull(string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        try
        {
            return Check.NotNull(value, parameterName, maxLength, minLength);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static string NotNullOrWhiteSpace(string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        try
        {
            return Check.NotNullOrWhiteSpace(value, parameterName, maxLength, minLength);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static string NotNullOrEmpty(string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength = int.MaxValue, int minLength = 0)
    {
        try
        {
            return Check.NotNullOrEmpty(value, parameterName, maxLength, minLength);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static ICollection<T> NotNullOrEmpty<T>(ICollection<T> value, [InvokerParameterName] [NotNull] string parameterName)
    {
        try
        {
            return Check.NotNullOrEmpty(value, parameterName);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static string Length([CanBeNull] string value, [InvokerParameterName] [NotNull] string parameterName, int maxLength, int minLength = 0)
    {
        try
        {
            return Check.Length(value, parameterName, maxLength, minLength);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static short Range(short value, [InvokerParameterName] [NotNull] string parameterName, short minimumValue, short maximumValue = short.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static int Range(int value, [InvokerParameterName] [NotNull] string parameterName, int minimumValue, int maximumValue = int.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static long Range(long value, [InvokerParameterName] [NotNull] string parameterName, long minimumValue, long maximumValue = long.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static float Range(float value, [InvokerParameterName] [NotNull] string parameterName, float minimumValue, float maximumValue = float.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static double Range(double value, [InvokerParameterName] [NotNull] string parameterName, double minimumValue, double maximumValue = double.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static decimal Range(decimal value, [InvokerParameterName] [NotNull] string parameterName, decimal minimumValue, decimal maximumValue = decimal.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static T NotDefaultOrNull<T>(T? value, [InvokerParameterName] [NotNull] string parameterName)
        where T : struct
    {
        try
        {
            return Check.NotDefaultOrNull(value, parameterName);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }
}