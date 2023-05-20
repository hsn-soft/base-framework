using System;
using System.Runtime.Serialization;
using HsnSoft.Base.ExceptionHandling;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Authorization;

/// <summary>
/// This exception is thrown on an unauthorized request.
/// </summary>
[Serializable]
public class BaseAuthorizationException : BaseException, IHasLogLevel, IHasErrorCode
{
    /// <summary>
    /// Creates a new <see cref="BaseAuthorizationException"/> object.
    /// </summary>
    public BaseAuthorizationException()
    {
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// Creates a new <see cref="BaseAuthorizationException"/> object.
    /// </summary>
    public BaseAuthorizationException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BaseAuthorizationException"/> object.
    /// </summary>
    /// <param name="message">Exception message</param>
    public BaseAuthorizationException(string message)
        : base(message)
    {
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// Creates a new <see cref="BaseAuthorizationException"/> object.
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public BaseAuthorizationException(string message, Exception innerException)
        : base(message, innerException)
    {
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// Creates a new <see cref="BaseAuthorizationException"/> object.
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="code">Exception code</param>
    /// <param name="innerException">Inner exception</param>
    public BaseAuthorizationException(string message = null, string code = null, Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = code;
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// Error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Severity of the exception.
    /// Default: Warn.
    /// </summary>
    public LogLevel LogLevel { get; set; }

    public BaseAuthorizationException WithData(string name, object value)
    {
        Data[name] = value;
        return this;
    }
}