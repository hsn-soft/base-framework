using System;
using System.Diagnostics;
using System.Text;
using HsnSoft.Base.Logging.Models;

namespace HsnSoft.Base.Logging;

public static class LogHelper
{
    /// <summary>Makes a structured log; with an exception it carries the messages of the whole chain and where it was thrown.</summary>
    public static FrameworkLogModel Generate(string message, object? reference = null, string? facility = null, string? correlationId = null, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        FrameworkLogModel result = new()
        {
            CorrelationId = correlationId,
            Facility = facility,
            Message = message,
            Reference = reference,
        };

        if (exception is null)
        {
            return result;
        }

        StackFrame? frame = new StackTrace(exception, true).GetFrame(0);
        result.StackTrace = new StackTraceLogDetail
        {
            ErrorDetail = MessagesOf(exception),
            StackFileName = frame?.GetFileName(),
            StackMethodName = frame?.GetMethod()?.Name,
            StackLineNumber = frame?.GetFileLineNumber() ?? 0,
        };
        return result;
    }

    // the message of the exception, then the messages of its inner exceptions, separated by a space
    private static string MessagesOf(Exception exception)
    {
        StringBuilder builder = new(exception.Message);
        for (Exception? inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(inner.Message))
            {
                builder.Append(' ').Append(inner.Message);
            }
        }

        return builder.ToString();
    }
}
