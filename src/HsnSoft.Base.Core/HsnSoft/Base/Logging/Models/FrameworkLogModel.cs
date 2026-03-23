using System;
using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;

namespace HsnSoft.Base.Logging.Models;

public sealed class FrameworkLogModel : IFrameworkLog
{
    [NotNull]
    public string LogId { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [NotNull]
    public string Message { get; set; } = string.Empty;

    [CanBeNull]
    public string CorrelationId { get; set; } /*HttpContext CorrelationId*/

    [CanBeNull]
    public string Facility { get; set; }

    [CanBeNull]
    public object Reference { get; set; }

    [CanBeNull]
    public StackTraceLogDetail StackTrace { get; set; }
}