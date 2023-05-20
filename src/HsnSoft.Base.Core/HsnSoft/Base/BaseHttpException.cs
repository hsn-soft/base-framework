using System;
using HsnSoft.Base.ExceptionHandling;

namespace HsnSoft.Base;

public sealed class BaseHttpException : Exception, IHasHttpStatusCode
{
    public int HttpStatusCode { get; }

    private BaseHttpException(string message = null) : base(message)
    {
    }

    public BaseHttpException(int statusCode, string message = null) : this(message)
    {
        HttpStatusCode = statusCode;
    }

    public BaseHttpException WithData(string name, object value)
    {
        Data[name] = value;
        return this;
    }
}