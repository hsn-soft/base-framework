using System;
using System.Collections.Generic;

namespace HsnSoft.Base;

/// <summary>Fails a request with an HTTP status the code chooses itself.</summary>
public sealed class BaseHttpException(int statusCode, string? message = null) : Exception(message ?? string.Empty), ILocalizableError
{
    public int HttpStatusCode { get; } = statusCode;

    public string? MessageKey { get; private set; }

    public IReadOnlyList<object?> MessageArgs { get; private set; } = [];

    /// <summary>Names the text the client gets and the values that go into it (<c>{0}</c>, <c>{1}</c>).</summary>
    public BaseHttpException WithKey(string messageKey, params object?[] args)
    {
        MessageKey = messageKey;
        MessageArgs = args;
        return this;
    }
}
