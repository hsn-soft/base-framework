using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace HsnSoft.Base.Logging;

/// <summary>
/// The two fields every log of a failure has besides the type of the exception: where it was thrown, and why, with everything its inner
/// exceptions say. They are built from the exception itself, so the logs of a request, of a message and of the exception handler tell the
/// same thing.
/// </summary>
public static class ExceptionLogFields
{
    public const string ExceptionIdentifier = nameof(ExceptionIdentifier);
    public const string Reason = nameof(Reason);

    /// <summary>The longest <c>Reason</c>; a longer one is cut and the cut is marked.</summary>
    public const int MaxReasonLength = 8192;

    private const string MessageSeparator = " >> ";

    /// <summary>Adds <c>ExceptionType</c> (the platform adds the rest of what the exception has), <c>ExceptionIdentifier</c> and <c>Reason</c>.</summary>
    public static void AddTo(IDictionary<string, object?> fields, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(exception);

        fields["ExceptionType"] = exception.GetType().FullName;
        fields[ExceptionIdentifier] = Identifier(exception);
        fields[Reason] = ReasonOf(exception);
    }

    /// <summary>
    /// Where it started, as <c>Solution:Acme|Assembly:Acme.OrderService.Http.Host|File:OrdersController.cs|Line:92</c>: the place in the code of the first
    /// frame that has a source file, looking at the innermost exception (the cause) first. Without symbols there is no file: the method is told
    /// instead, and an exception that was never thrown has only its type.
    /// </summary>
    public static string Identifier(Exception exception) => Identifier(exception, null);

    /// <param name="exception">The exception.</param>
    /// <param name="solution">
    /// The solution whose code is wanted (the first part of its assemblies' names): the place in <em>that</em> code, not in a library that has
    /// its sources in its symbols. By default the solution of the application that is running.
    /// </param>
    public static string Identifier(Exception exception, string? solution)
    {
        ArgumentNullException.ThrowIfNull(exception);

        List<Exception> chain = Chain(exception);
        string wanted = solution ?? SolutionOf(Assembly.GetEntryAssembly()?.GetName().Name ?? AppDomain.CurrentDomain.FriendlyName);

        // the code of this application first (a throw deep in a library is reported where the application called it), then any code with sources
        foreach (bool onlyWanted in new[] { true, false })
        {
            for (int index = chain.Count - 1; index >= 0; index--)
            {
                foreach (StackFrame frame in new StackTrace(chain[index], fNeedFileInfo: true).GetFrames())
                {
                    if (frame.GetFileName() is { Length: > 0 } file && AssemblyOf(frame.GetMethod()) is { } assembly && (!onlyWanted || SolutionOf(assembly) == wanted))
                    {
                        return string.Create(CultureInfo.InvariantCulture, $"Solution:{SolutionOf(assembly)}|Assembly:{assembly}|File:{Path.GetFileName(file)}|Line:{frame.GetFileLineNumber()}");
                    }
                }
            }
        }

        Exception cause = chain[^1];
        MethodBase? method = new StackTrace(cause, fNeedFileInfo: false).GetFrames() is { Length: > 0 } frames ? frames[0].GetMethod() : null;
        string name = AssemblyOf(method) ?? cause.GetType().Assembly.GetName().Name ?? "unknown";
        return method is null
            ? $"Solution:{SolutionOf(name)}|Assembly:{name}|Type:{cause.GetType().FullName}"
            : $"Solution:{SolutionOf(name)}|Assembly:{name}|Method:{method.DeclaringType?.FullName}.{method.Name}";
    }

    /// <summary>The message of the exception and of every exception inside it, down to the one that has none inside, joined with <c>" >> "</c>.</summary>
    public static string ReasonOf(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        StringBuilder reason = new();
        foreach (Exception current in Chain(exception))
        {
            if (reason.Length > 0)
            {
                reason.Append(MessageSeparator);
            }

            reason.Append(current.Message);
        }

        return LogFields.Text(reason.ToString(), MaxReasonLength)!;
    }

    private static List<Exception> Chain(Exception exception)
    {
        List<Exception> chain = [];
        for (Exception? current = exception; current is not null && !chain.Contains(current); current = current.InnerException)
        {
            chain.Add(current);
        }

        return chain;
    }

    private static string? AssemblyOf(MethodBase? method) => method?.DeclaringType?.Assembly.GetName().Name;

    private static string SolutionOf(string assembly) => assembly.Split('.')[0];
}
