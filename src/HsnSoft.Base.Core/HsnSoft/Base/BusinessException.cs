using System;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base;

/// <summary>
/// A business rule the application itself defines and names: <see cref="ErrorCode"/> says which, so a client can react to it. Hosting
/// layers answer it as "bad request" (HTTP 400) with the code.
/// </summary>
public class BusinessException(string? errorCode = null, string? errorMessage = null, Exception? innerException = null, LogLevel logLevel = LogLevel.Warning)
    : Exception(errorMessage ?? string.Empty, innerException)
{
    public string? ErrorCode { get; set; } = errorCode;

    public LogLevel LogLevel { get; set; } = logLevel;

    /// <summary>Adds a named value that is shown with the error (the field that was wrong, the limit that was exceeded).</summary>
    public BusinessException WithData(string name, object? value)
    {
        Data[name] = value;
        return this;
    }
}
