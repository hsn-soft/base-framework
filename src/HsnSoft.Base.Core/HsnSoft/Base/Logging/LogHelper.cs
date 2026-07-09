using System;
using System.Diagnostics;
using System.Linq;
using HsnSoft.Base.Logging.Models;
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
            Message = message,
            Reference = reference,
            StackTrace = null
        };

        if (exception == null) return result;

        string exceptionDetail = exception.Message;
        if (exception.InnerException != null)
        {
            var errors = exception.InnerException.GetMessages().ToList();
            exceptionDetail += " " + string.Join(' ', errors);
        }
        var stackFrame = new StackTrace(exception, true).GetFrame(0);
        result.StackTrace = new StackTraceLogDetail
        {
            ErrorDetail = exceptionDetail,
            StackFileName = stackFrame?.GetFileName(),
            StackMethodName = stackFrame?.GetMethod()?.Name,
            StackLineNumber = stackFrame?.GetFileLineNumber() ?? 0
        };

        return result;
    }
}