using System;

namespace HsnSoft.Base;

public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message ?? string.Empty)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message ?? string.Empty, innerException)
    {
    }

    public DomainException WithData(string name, object value)
    {
        Data[name] = value;
        return this;
    }
}