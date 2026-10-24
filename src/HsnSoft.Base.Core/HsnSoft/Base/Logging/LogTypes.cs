namespace HsnSoft.Base.Logging;

/// <summary>
/// The <c>LogType</c> field of a log: which kind of log it is, so Graylog can tell the logs of the framework, the logs that
/// follow a request or a message through the system, and the logs of the application apart.
/// </summary>
public static class LogTypes
{
    /// <summary>A log of a framework library (<c>HsnSoft.Base.*</c>) that nobody wrote on purpose: its <c>Facility</c> says what it is about.</summary>
    public const string FrameworkLog = "FrameworkLog";

    /// <summary>A log of anything that is neither the framework nor the application (ASP.NET Core, Entity Framework, a database driver, any library).</summary>
    public const string PlatformLog = "PlatformLog";

    /// <summary>A log of the shared library of the application (the classes under <c>{AppNamespace}.Shared</c>: the policies and helpers every service of a product uses).</summary>
    public const string SharedLog = "SharedLog";

    /// <summary>A log written by the code of the application (the classes under <c>FrameworkLogger:AppNamespace</c>) with <c>ILogger</c>.</summary>
    public const string AppLog = "AppLog";

    /// <summary>The start of an HTTP request. Pairs with <see cref="HttpResponse"/> through <c>ScopeId</c>.</summary>
    public const string HttpRequest = "HttpRequest";

    /// <summary>The end of an HTTP request, with its status and how long it took.</summary>
    public const string HttpResponse = "HttpResponse";

    /// <summary>
    /// One log for a whole request that a gateway answered itself (a 401, a 429, a 404, a 502 because the service could not be reached): the
    /// service that would have logged <see cref="HttpRequest"/> and <see cref="HttpResponse"/> never saw it.
    /// </summary>
    public const string HttpRequestResponse = "HttpRequestResponse";

    /// <summary>The start of the handling of a message. Pairs with <see cref="ConsumeEnd"/> through <c>ScopeId</c>.</summary>
    public const string ConsumeStart = "ConsumeStart";

    /// <summary>The end of the handling of a message, with its result and how long it took.</summary>
    public const string ConsumeEnd = "ConsumeEnd";
}
