using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace HsnSoft.Base.Logging;

/// <summary>
/// The two fields every log of a failure has besides the type of the exception: where it was thrown, and why, with everything its inner
/// exceptions say. They are built from the exception itself, so the logs of a request, of a message and of the exception handler tell the
/// same thing.
/// </summary>
public static partial class ExceptionLogFields
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
    /// Where it started, as <c>Solution:Xyz|Assembly:Xyz.ServiceOrder.Host|File:OrdersController.cs|Line:92|Method:OrdersController.PlaceAsync</c>: the place in the code of the first
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
                        return string.Create(CultureInfo.InvariantCulture, $"Solution:{SolutionOf(assembly)}|Assembly:{assembly}|File:{Path.GetFileName(file)}|Line:{frame.GetFileLineNumber()}|Method:{MethodName(frame.GetMethod()!)}");
                    }
                }
            }
        }

        Exception cause = chain[^1];
        MethodBase? method = new StackTrace(cause, fNeedFileInfo: false).GetFrames() is { Length: > 0 } frames ? frames[0].GetMethod() : null;
        string name = AssemblyOf(method) ?? cause.GetType().Assembly.GetName().Name ?? "unknown";
        return method is null
            ? $"Solution:{SolutionOf(name)}|Assembly:{name}|Type:{cause.GetType().FullName}"
            : $"Solution:{SolutionOf(name)}|Assembly:{name}|Method:{MethodName(method)}";
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

    /// <summary>
    /// <c>OrderRepository.InsertAsync</c>: the type and the method the developer wrote. The compiler turns an async method into a class (<c>&lt;InsertAsync&gt;d__5</c>)
    /// and a lambda into a method of a hidden class (<c>&lt;Handle&gt;b__0</c>): the name of what was written is taken out of those.
    /// </summary>
    internal static string MethodName(MethodBase method)
    {
        Type? type = method.DeclaringType;
        string name = method.Name;

        // a state machine is named after its method; a lambda or a local function is a method named after the method it is in
        string source = type is not null && typeof(IAsyncStateMachine).IsAssignableFrom(type) ? type.Name : name;
        if (HiddenName().Match(source) is { Success: true } match)
        {
            name = match.Groups["name"].Value;
        }

        // the hidden classes of the compiler (closures, state machines) live inside the class that was written
        while (type is not null && (type.Name.StartsWith('<') || type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)))
        {
            type = type.DeclaringType;
        }

        return type is null ? name : $"{type.Name}.{name}";
    }

    [GeneratedRegex("^<+(?<name>[^<>]+)>")]
    private static partial Regex HiddenName();

    private static string? AssemblyOf(MethodBase? method) => method?.DeclaringType?.Assembly.GetName().Name;

    private static string SolutionOf(string assembly) => assembly.Split('.')[0];
}
