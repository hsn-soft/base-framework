using System;

namespace HsnSoft.Base;

/// <summary>Fails a request with an HTTP status the code chooses itself.</summary>
public sealed class BaseHttpException(int statusCode, string? message = null) : Exception(message ?? string.Empty)
{
    public int HttpStatusCode { get; } = statusCode;
}
