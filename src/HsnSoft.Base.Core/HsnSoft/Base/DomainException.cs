using System;
using System.Collections.Generic;

namespace HsnSoft.Base;

/// <summary>A rule of the domain model was broken. Hosting layers answer it as "bad request" (HTTP 400).</summary>
public class DomainException : Exception, ILocalizableError
{
    public DomainException()
    {
    }

    public DomainException(string? message)
        : base(message ?? string.Empty)
    {
    }

    public DomainException(string? message, Exception? innerException)
        : base(message ?? string.Empty, innerException)
    {
    }

    public string? MessageKey { get; private set; }

    public IReadOnlyList<object?> MessageArgs { get; private set; } = [];

    /// <summary>Names the text the client gets and the values that go into it (<c>{0}</c>, <c>{1}</c>).</summary>
    public DomainException WithKey(string messageKey, params object?[] args)
    {
        MessageKey = messageKey;
        MessageArgs = args;
        return this;
    }

    public DomainException WithData(string name, object? value)
    {
        Data[name] = value;
        return this;
    }
}
