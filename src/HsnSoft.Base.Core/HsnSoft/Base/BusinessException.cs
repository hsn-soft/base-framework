using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base;

/// <summary>
/// A business rule the application itself defines and names: <see cref="ErrorCode"/> says which, so a client can react to it. Hosting
/// layers answer it as "bad request" (HTTP 400) with the code.
/// </summary>
public class BusinessException(string? errorCode = null, string? errorMessage = null, Exception? innerException = null, LogLevel logLevel = LogLevel.Warning)
    : Exception(errorMessage ?? string.Empty, innerException), ILocalizableError
{
    public string? ErrorCode { get; set; } = errorCode;

    /// <summary>The key of the text the client gets: the error code unless <see cref="WithKey"/> gave another.</summary>
    public string? MessageKey { get => field ?? ErrorCode; private set; }

    public IReadOnlyList<object?> MessageArgs { get; private set; } = [];

    public LogLevel LogLevel { get; set; } = logLevel;

    /// <summary>Names the text the client gets and the values that go into it (<c>{0}</c>, <c>{1}</c>); without it the error code is the key.</summary>
    public BusinessException WithKey(string messageKey, params object?[] args)
    {
        MessageKey = messageKey;
        MessageArgs = args;
        return this;
    }

    /// <summary>Adds a named value that is shown with the error (the field that was wrong, the limit that was exceeded).</summary>
    public BusinessException WithData(string name, object? value)
    {
        Data[name] = value;
        return this;
    }
}
