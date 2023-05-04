using System;

namespace HsnSoft.Base.Data;

public class BaseDbConcurrencyException : BaseException
{
    /// <summary>
    /// Creates a new <see cref="BaseDbConcurrencyException"/> object.
    /// </summary>
    public BaseDbConcurrencyException()
    {
    }

    /// <summary>
    /// Creates a new <see cref="BaseDbConcurrencyException"/> object.
    /// </summary>
    /// <param name="message">Exception message</param>
    public BaseDbConcurrencyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BaseDbConcurrencyException"/> object.
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public BaseDbConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}