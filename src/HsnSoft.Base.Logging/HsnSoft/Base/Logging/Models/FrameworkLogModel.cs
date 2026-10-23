using System;
using HsnSoft.Base.Logging.Abstracts;

namespace HsnSoft.Base.Logging.Models;

public sealed class FrameworkLogModel : IFrameworkLog
{
    public string LogId { get; set; } = Guid.CreateVersion7().ToString();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string Message { get; set; } = string.Empty;

    /// <summary>The correlation id of the request (or of the message chain) the log belongs to.</summary>
    public string? CorrelationId { get; set; }

    public string? Facility { get; set; }

    public object? Reference { get; set; }

    public StackTraceLogDetail? StackTrace { get; set; }
}
