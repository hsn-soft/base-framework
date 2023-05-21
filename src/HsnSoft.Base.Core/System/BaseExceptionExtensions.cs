using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.ExceptionServices;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;

namespace System;

/// <summary>
/// Extension methods for <see cref="Exception"/> class.
/// </summary>
public static class BaseExceptionExtensions
{
    /// <summary>
    /// Uses <see cref="ExceptionDispatchInfo.Capture"/> method to re-throws exception
    /// while preserving stack trace.
    /// </summary>
    /// <param name="exception">Exception to be re-thrown</param>
    public static void ReThrow(this Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>
    /// Try to get a log level from the given <paramref name="exception"/>
    /// if it implements the <see cref="IHasLogLevel"/> interface.
    /// Otherwise, returns the <paramref name="defaultLevel"/>.
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="defaultLevel"></param>
    /// <returns></returns>
    public static LogLevel GetLogLevel(this Exception exception, LogLevel defaultLevel = LogLevel.Error)
    {
        return (exception as IHasLogLevel)?.LogLevel ?? defaultLevel;
    }

    public static IEnumerable<string> GetMessages(this Exception ex)
    {
        var messages = new List<string>();

        if (ex == null) return messages;
        var currentExc = ex;
        do
        {
            if (!string.IsNullOrWhiteSpace(currentExc.Message)) messages.Add(currentExc.Message);

            currentExc = currentExc.InnerException;
        } while (currentExc != null);

        return messages;
    }

    public static IEnumerable<string> GetStringDataList(this Exception ex)
    {
        var messages = new List<string>();

        if (ex == null) return messages;

        if (ex.Data is not { Count: > 0 }) return messages;
      
        var keys = ex.Data.Keys.ToDynamicList<string>().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        messages.AddRange(from key in keys let dataValue = ex.Data[key] as string where !string.IsNullOrWhiteSpace(dataValue) select $"{key}:{dataValue}");

        return messages;
    }
}