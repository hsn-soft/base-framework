using System;
using System.Collections.Generic;

namespace HsnSoft.Base;

/// <summary>The requested resource does not exist. Hosting layers answer it as "not found" (HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message), ILocalizableError
{
    public string? MessageKey { get; private set; }

    public IReadOnlyList<object?> MessageArgs { get; private set; } = [];

    /// <summary>Names the text the client gets and the values that go into it (<c>{0}</c>, <c>{1}</c>).</summary>
    public NotFoundException WithKey(string messageKey, params object?[] args)
    {
        MessageKey = messageKey;
        MessageArgs = args;
        return this;
    }
}

/// <summary>A business rule rejected the request. Hosting layers answer it as "bad request" (HTTP 400).</summary>
public sealed class BusinessRuleException(string message) : Exception(message), ILocalizableError
{
    public string? MessageKey { get; private set; }

    public IReadOnlyList<object?> MessageArgs { get; private set; } = [];

    /// <summary>Names the text the client gets and the values that go into it (<c>{0}</c>, <c>{1}</c>).</summary>
    public BusinessRuleException WithKey(string messageKey, params object?[] args)
    {
        MessageKey = messageKey;
        MessageArgs = args;
        return this;
    }
}
