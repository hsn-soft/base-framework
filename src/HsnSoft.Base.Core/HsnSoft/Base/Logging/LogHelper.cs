using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace HsnSoft.Base.Logging;

public static class LogHelper
{
    public static FrameworkLogModel Generate([NotNull] string message, object reference = null, [CanBeNull] string facility = null, [CanBeNull] string correlationId = null, Exception exception = null)
    {
        var result = new FrameworkLogModel
        {
            CorrelationId = correlationId,
            Facility = facility,
            Description = message,
            Reference = reference,
            StackTrace = null
        };

        if (exception == null) return result;

        var stackFrame = new StackTrace(exception, true).GetFrame(0);
        result.StackTrace = new StackTraceLogDetail
        {
            StackFileName = stackFrame?.GetFileName(),
            StackMethodName = stackFrame?.GetMethod()?.Name,
            StackLineNumber = stackFrame?.GetFileLineNumber() ?? 0
        };

        return result;
    }
}