using System;
using System.Runtime.Serialization;

namespace HsnSoft.Base.Core;

/// <summary>
/// Base exception type for those are thrown by Base system for Base specific exceptions.
/// </summary>
public class BaseException : Exception
{
    public BaseException()
    {
    }

    public BaseException(string message)
        : base(message)
    {
    }

    public BaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BaseException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {
    }
}