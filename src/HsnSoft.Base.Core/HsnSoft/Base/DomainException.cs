using System;

namespace HsnSoft.Base;

/// <summary>A rule of the domain model was broken. Hosting layers answer it as "bad request" (HTTP 400).</summary>
public class DomainException : Exception
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

    public DomainException WithData(string name, object? value)
    {
        Data[name] = value;
        return this;
    }
}
