using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public static Int16 Range(Int16 value, [InvokerParameterName] [NotNull] string parameterName, Int16 minimumValue, Int16 maximumValue = Int16.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static Int32 Range(Int32 value, [InvokerParameterName] [NotNull] string parameterName, Int32 minimumValue, Int32 maximumValue = Int32.MaxValue)
    {
        try
        {
            return Check.Range(value, parameterName, minimumValue, maximumValue);
        }
        catch (Exception e) { throw new DomainException(e.Message, e.InnerException); }
    }

    public static Int64 Range(Int64 value, [InvokerParameterName] [NotNull] string parameterName, Int64 minimumValue, Int64 maximumValue = Int64.MaxValue)
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