using System;
using System.Collections.Generic;

namespace HsnSoft.Base;

/// <summary>Fails a request with an HTTP status the code chooses itself. A kind of failure with more to say derives from it (<c>RepositoryException</c> of the domain) and the hosting layers answer it with the status, the text and the error code it names.</summary>
public class BaseHttpException(int statusCode, string? message = null, Exception? innerException = null) : Exception(message ?? string.Empty, innerException), ILocalizableError
{
    public int HttpStatusCode { get; } = statusCode;

    public string? MessageKey { get; private set; }

    public IReadOnlyList<object?> MessageArgs { get; private set; } = [];

    /// <summary>A stable code the client can decide on, whatever the language of the text (<c>entity_not_found</c>); <see langword="null"/> when the exception names none.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>Names the stable code of the failure (the <c>errorCode</c> of the response).</summary>
    public BaseHttpException WithCode(string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        ErrorCode = errorCode;
        return this;
    }

    /// <summary>Names the text the client gets and the values that go into it (<c>{0}</c>, <c>{1}</c>).</summary>
    public BaseHttpException WithKey(string messageKey, params object?[] args)
    {
        MessageKey = messageKey;
        MessageArgs = args;
        return this;
    }
}
