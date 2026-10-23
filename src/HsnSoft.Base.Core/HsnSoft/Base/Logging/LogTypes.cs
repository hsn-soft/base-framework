namespace HsnSoft.Base.Logging;

/// <summary>
/// The <c>LogType</c> field of a log: which kind of log it is, so Graylog can tell the logs of the framework, the logs that
/// follow a request or a message through the system, and the logs of the application apart.
/// </summary>
public static class LogTypes
{
    /// <summary>A log of a framework library (<c>HsnSoft.Base.*</c>) that nobody wrote on purpose: its <c>Facility</c> says what it is about.</summary>
    public const string FrameworkLog = "FrameworkLog";

    /// <summary>A log of the platform or a third-party library (ASP.NET Core, Entity Framework, a database driver).</summary>
    public const string PlatformLog = "PlatformLog";

    /// <summary>A log the application's developer wrote with <c>ILogger</c>.</summary>
    public const string AppLog = "AppLog";

    /// <summary>The start of an HTTP request. Pairs with <see cref="HttpResponse"/> through <c>ScopeId</c>.</summary>
    public const string HttpRequest = "HttpRequest";

    /// <summary>The end of an HTTP request, with its status and how long it took.</summary>
    public const string HttpResponse = "HttpResponse";

    /// <summary>The start of the handling of a message. Pairs with <see cref="ConsumeEnd"/> through <c>ScopeId</c>.</summary>
    public const string ConsumeStart = "ConsumeStart";

    /// <summary>The end of the handling of a message, with its result and how long it took.</summary>
    public const string ConsumeEnd = "ConsumeEnd";
}
